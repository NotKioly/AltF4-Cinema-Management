using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

namespace AltF4.Cinema.Core;

public static class Passwords
{
    public static void Validate(string value)
    {
        if (value.Length<10 || value.Length>128 || !value.Any(char.IsLetter) || !value.Any(char.IsDigit))
            throw new InvalidOperationException("Mật khẩu cần 10–128 ký tự, gồm chữ và số.");
    }
    public static string Hash(string value)
    {
        Validate(value);
        var salt=RandomNumberGenerator.GetBytes(16);
        var hash=Rfc2898DeriveBytes.Pbkdf2(value,salt,300000,HashAlgorithmName.SHA256,32);
        return $"pbkdf2-sha256$300000${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }
    public static bool Verify(string value,string encoded)
    {
        try { var p=encoded.Split('$'); if(p.Length!=4 || p[0]!="pbkdf2-sha256") return false; var rounds=int.Parse(p[1]); if(rounds<100000 || rounds>1000000) return false;
            return CryptographicOperations.FixedTimeEquals(Convert.FromBase64String(p[3]),Rfc2898DeriveBytes.Pbkdf2(value,Convert.FromBase64String(p[2]),rounds,HashAlgorithmName.SHA256,32)); }
        catch { return false; }
    }
}

public sealed class SecurityService(Func<CinemaDb> factory)
{
    public Actor Login(string username,string password)
    {
        using var db=factory();
        var u=db.Users.Include(x=>x.Role).SingleOrDefault(x=>x.Username==username.Trim().ToLowerInvariant());
        if(u==null || !u.Active || u.LockedUntil>DateTime.Now) throw new InvalidOperationException("Tài khoản hoặc mật khẩu không hợp lệ, hoặc tài khoản đang bị khóa.");
        if(!Passwords.Verify(password,u.PasswordHash)) {
            u.FailedAttempts++; if(u.FailedAttempts>=5) { u.LockedUntil=DateTime.Now.AddMinutes(5);u.FailedAttempts=0; }
            db.AuditEntries.Add(new(){At=DateTime.Now,UserId=u.Id,Action="LOGIN_FAILED",Detail="Đăng nhập không thành công"});db.SaveChanges();
            throw new InvalidOperationException("Tài khoản hoặc mật khẩu không hợp lệ, hoặc tài khoản đang bị khóa.");
        }
        u.FailedAttempts=0;u.LockedUntil=null;
        db.AuditEntries.Add(new(){At=DateTime.Now,UserId=u.Id,Action="LOGIN",Detail=u.Username});db.SaveChanges();
        return new(u.Id,u.FullName);
    }
    public static User Require(CinemaDb db, Actor actor, Access permission)
    {
        var user=db.Users.Include(x=>x.Role).SingleOrDefault(x=>x.Id==actor.UserId);
        if(user==null || !user.Active || (user.Role.Permissions&permission)!=permission) throw new UnauthorizedAccessException("Bạn không có quyền thực hiện chức năng này.");
        if(user.MustChangePassword) throw new UnauthorizedAccessException("Bạn cần đổi mật khẩu ban đầu trước khi sử dụng.");
        return user;
    }
    public Access Permissions(Actor a) { using var db=factory();var u=db.Users.Include(x=>x.Role).Single(x=>x.Id==a.UserId);return u.Active ? u.Role.Permissions : Access.None; }
    public bool MustChange(Actor a) {using var db=factory();return db.Users.Single(x=>x.Id==a.UserId).MustChangePassword;}
    public void ChangePassword(Actor a,string oldPassword,string newPassword)
    {
        using var db=factory();var u=db.Users.Single(x=>x.Id==a.UserId);
        if(!u.Active || !Passwords.Verify(oldPassword,u.PasswordHash)) throw new InvalidOperationException("Mật khẩu hiện tại không đúng.");
        if(Passwords.Verify(newPassword,u.PasswordHash)) throw new InvalidOperationException("Mật khẩu mới phải khác mật khẩu cũ.");
        u.PasswordHash=Passwords.Hash(newPassword);u.MustChangePassword=false;
        db.AuditEntries.Add(new(){At=DateTime.Now,UserId=a.UserId,Action="PASSWORD_CHANGED",Detail=u.Username});db.SaveChanges();
    }
}
