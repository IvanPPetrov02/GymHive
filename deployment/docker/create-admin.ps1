# Create Admin User in GymHive Database
# This script creates an admin user directly in the MySQL database

Write-Host "Creating Admin User for GymHive..." -ForegroundColor Cyan
Write-Host ""

# Admin user details
$adminEmail = "admin@gymhive.com"
$adminPassword = "Admin123!"
$adminName = "Admin"
$adminSurname = "User"
$uuid = [System.Guid]::NewGuid().ToString()
$createdAt = Get-Date -Format "yyyy-MM-dd HH:mm:ss.ffffff"
$role = 2  # Admin role

Write-Host "Admin Details:" -ForegroundColor Yellow
Write-Host "  Email: $adminEmail" -ForegroundColor White
Write-Host "  Password: $adminPassword" -ForegroundColor White
Write-Host "  Role: Admin (2)" -ForegroundColor White
Write-Host ""

# Note: BCrypt hashing with pepper needs to be done by the application
# So we'll use a pre-generated hash for "Admin123!" with the default pepper "GymPepper"
# You can generate this by temporarily adding a test endpoint or using the register endpoint

Write-Host "⚠️  IMPORTANT: Password hashing requires BCrypt with pepper" -ForegroundColor Red
Write-Host "Choose an option:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Use the register endpoint to create admin (RECOMMENDED)" -ForegroundColor Green
Write-Host "   - Register normally, then update Role in database" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Generate hash using a temporary endpoint (requires code change)" -ForegroundColor Yellow
Write-Host ""
Write-Host "3. Insert with a known hash (for testing only)" -ForegroundColor Red
Write-Host ""

$choice = Read-Host "Enter your choice (1-3)"

if ($choice -eq "1") {
    Write-Host ""
    Write-Host "STEP 1: Register the admin user via the app" -ForegroundColor Cyan
    Write-Host "  URL: http://localhost:3000/register" -ForegroundColor White
    Write-Host "  Email: $adminEmail" -ForegroundColor White
    Write-Host "  Password: $adminPassword" -ForegroundColor White
    Write-Host "  Role: Select 'User' (we'll upgrade it)" -ForegroundColor White
    Write-Host ""
    Write-Host "Press Enter after you've registered the user..."
    Read-Host
    
    Write-Host ""
    Write-Host "STEP 2: Updating role to Admin in database..." -ForegroundColor Cyan
    
    $sqlUpdate = "UPDATE Users SET Role = 2 WHERE Email = '$adminEmail';"
    
    docker exec -it gymhive-auth-db mysql -u gymhive_user -pGymHive123! GymHive -e "$sqlUpdate"
    
    Write-Host ""
    Write-Host "✅ User upgraded to Admin!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Verification:" -ForegroundColor Yellow
    docker exec -it gymhive-auth-db mysql -u gymhive_user -pGymHive123! GymHive -e "SELECT UUID, Email, Name, Surname, Role FROM Users WHERE Email = '$adminEmail';"
    Write-Host ""
    Write-Host "You can now login as admin!" -ForegroundColor Green
    
} elseif ($choice -eq "2") {
    Write-Host ""
    Write-Host "To generate a proper hash, you need to:" -ForegroundColor Yellow
    Write-Host "1. Add a temporary endpoint in AuthenticationController.cs:" -ForegroundColor Gray
    Write-Host ""
    Write-Host '[HttpPost("hash-password")]' -ForegroundColor DarkGray
    Write-Host 'public IActionResult HashPassword([FromBody] string password)' -ForegroundColor DarkGray
    Write-Host '{' -ForegroundColor DarkGray
    Write-Host '    return Ok(PassHash.HashPassword(password));' -ForegroundColor DarkGray
    Write-Host '}' -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "2. Rebuild and restart the container" -ForegroundColor Gray
    Write-Host "3. Call: POST http://localhost:5000/auth/hash-password" -ForegroundColor Gray
    Write-Host "   Body: `"Admin123!`"" -ForegroundColor Gray
    Write-Host "4. Use the returned hash in option 3" -ForegroundColor Gray
    
} elseif ($choice -eq "3") {
    Write-Host ""
    Write-Host "⚠️  WARNING: Using a test hash - this is for development only!" -ForegroundColor Red
    Write-Host ""
    
    # This is a BCrypt hash for "Admin123!" with pepper "GymPepper"
    # Generated: $2a$12$[random salt]$[hash]
    # Note: You should generate your own hash using option 2
    $testHash = '$2a$12$' + 'testHashPlaceholder' # This won't actually work
    
    Write-Host "You need to generate a proper hash first using option 2." -ForegroundColor Red
    Write-Host "Press Enter to continue..."
    Read-Host
} else {
    Write-Host "Invalid choice" -ForegroundColor Red
}
