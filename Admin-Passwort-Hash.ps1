# Windows PowerShell 5.1 / .NET Framework 4.7.2. Kein Passwort als Argument übergeben.
$ErrorActionPreference = 'Stop'
$a = Read-Host 'Neues gemeinsames Adminpasswort (20–128 Zeichen)' -AsSecureString
$b = Read-Host 'Passwort wiederholen' -AsSecureString
$pa = [IntPtr]::Zero; $pb = [IntPtr]::Zero; $derive = $null; $rng = $null
try {
 $pa = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($a)
 $pb = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($b)
 $first = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pa)
 $second = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pb)
 if ($first -cne $second -or $first.Length -lt 20 -or $first.Length -gt 128) { throw 'Passwörter müssen übereinstimmen und 20–128 Zeichen lang sein.' }
 $salt = New-Object byte[] 24
 $rng = [Security.Cryptography.RandomNumberGenerator]::Create(); $rng.GetBytes($salt)
 $derive = [Security.Cryptography.Rfc2898DeriveBytes]::new($first,$salt,600000,[Security.Cryptography.HashAlgorithmName]::SHA256)
 $hash = 'pbkdf2_sha256$600000$' + [Convert]::ToBase64String($salt) + '$' + [Convert]::ToBase64String($derive.GetBytes(32))
 Write-Host "`nDiesen HASH in Render als MAC_ADMIN_HASH eintragen (nicht auf GitHub):`n"
 Write-Output $hash
 Write-Host "`nDas gewählte Passwort selbst nur berechtigten Admins geben."
} finally {
 if ($pa -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pa) }
 if ($pb -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pb) }
 if ($derive) { $derive.Dispose() }; if ($rng) { $rng.Dispose() }
 $first=$second=$null; $a.Dispose(); $b.Dispose()
}
Read-Host 'Enter zum Schließen'
