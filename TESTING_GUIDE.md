# GymHive API Testing Guide

## 🚀 Quick Start

### 1. Start All Services

Run the startup script from the GymHive root directory:

```powershell
.\start-all-services.ps1
```

This will open 6 PowerShell windows, one for each service:
- **AuthenticationService** - Port 5010
- **GymService** - Port 5001  
- **MembershipService** - Port 5002
- **NotificationsService** - Port 5003
- **WorkoutLoggingService** - Port 5004
- **API Gateway** - Port 5000

**Wait 30-60 seconds** for all services to fully start and connect to databases.

### 2. Verify Services Are Running

Open browser and check:
- Gateway: http://localhost:5000 (should show service documentation page)
- Gateway Health: http://localhost:5000/health (should return JSON with all services)

### 3. Import Postman Collection

1. Open Postman
2. Click **Import** button
3. Select file: `GymHive-Postman-Collection.json`
4. Collection will be imported with all endpoints organized

---

## 📫 Postman Collection Structure

### Collection Variables
The collection uses these variables (auto-populated by tests):
- `gateway_url` = http://localhost:5000
- `auth_token` = (saved automatically after login)
- `user_id` = (saved automatically after registration)
- `gym_id` = 1 (default)
- `workout_log_id` = (saved automatically after check-in)

### Folders

#### 0. Gateway Health Check
- **Gateway Health** - Check if gateway is running
- **Gateway Landing Page** - View service documentation

#### 1. Authentication Service
- **Register Member** - Create new user account (auto-saves token)
- **Login** - Login with credentials (auto-saves token)
- **Get Current User** - Get logged-in user info

#### 2. Gym Service
- **Get All Gyms** - List all gyms
- **Get Gym By ID** - Get specific gym details
- **Search Gyms** - Search gyms by name/city

#### 3. Membership Service
- **Get My Memberships** - View user's memberships
- **Get Membership Plans for Gym** - View available plans
- **Purchase Membership** - Buy a membership plan

#### 4. Notifications Service
- **Get My Notifications** - View user notifications (paginated)
- **Get Unread Count** - Count unread notifications
- **Mark Notification as Read** - Mark single notification as read
- **Mark All as Read** - Mark all notifications as read
- **Delete Notification** - Remove notification

#### 5. Workout Logging Service
- **Check In to Gym** - Start workout session (auto-saves workout_log_id)
- **Get My Workout Logs** - View workout history (paginated)
- **Get Workout Statistics** - View workout stats (total workouts, minutes, etc.)
- **Check Out from Gym** - End workout session (calculates duration)

#### 6. Integration Tests
- **Full Member Journey** - Complete user flow test:
  1. Register new account
  2. Find a gym
  3. Check in to gym
  4. Check notifications (should have check-in notification)
  5. Check out from gym
  6. View workout statistics

---

## 🧪 Testing Workflow

### Basic Test Flow

**Run requests in this order:**

1. **Register or Login** (Authentication Service)
   - Use "Register Member" or "Login" request
   - Token will be saved automatically to `auth_token` variable
   - All subsequent requests will use this token

2. **Get Gyms** (Gym Service)
   - "Get All Gyms" to see available gyms
   - Note a gym ID (default is 1)

3. **Check In** (Workout Logging Service)
   - "Check In to Gym" with gym ID
   - Workout log ID saved automatically
   - Triggers notification event

4. **Check Notifications** (Notifications Service)
   - "Get My Notifications" to see check-in notification
   - "Get Unread Count" to see notification count

5. **Check Out** (Workout Logging Service)
   - "Check Out from Gym" using saved workout_log_id
   - Duration calculated automatically
   - Triggers workout logged event → notification

6. **View Statistics** (Workout Logging Service)
   - "Get Workout Statistics" to see aggregated data

### Advanced Testing

**Run the Full Member Journey** folder to execute complete integration test with automatic validation.

---

## 📊 Expected Responses

### Successful Check-In Response (200 OK)
```json
{
  "id": 1,
  "userId": "guid-here",
  "gymId": 1,
  "checkInTime": "2025-01-27T10:30:00Z",
  "checkOutTime": null,
  "duration": null,
  "createdAt": "2025-01-27T10:30:00Z"
}
```

### Successful Check-Out Response (200 OK)
```json
{
  "id": 1,
  "userId": "guid-here",
  "gymId": 1,
  "checkInTime": "2025-01-27T10:30:00Z",
  "checkOutTime": "2025-01-27T12:00:00Z",
  "duration": 90,
  "createdAt": "2025-01-27T10:30:00Z"
}
```

### Workout Statistics Response (200 OK)
```json
{
  "totalWorkouts": 5,
  "totalMinutes": 450,
  "averageDuration": 90.0,
  "lastWorkout": "2025-01-27T12:00:00Z",
  "workoutsThisWeek": 3,
  "workoutsThisMonth": 5
}
```

### Notifications Response (200 OK)
```json
[
  {
    "id": 1,
    "userId": "guid-here",
    "type": "CheckIn",
    "title": "Check-In Successful",
    "message": "You checked in at Gym #1",
    "isRead": false,
    "createdAt": "2025-01-27T10:30:00Z",
    "readAt": null
  }
]
```

---

## 🔐 Authentication

All requests (except register/login) require JWT Bearer token:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

The Postman collection automatically:
1. Saves token after successful login/register
2. Includes token in all protected requests
3. Uses `{{auth_token}}` variable

---

## 🐛 Troubleshooting

### Services Won't Start

**Check:**
1. MySQL is running on port 3306
2. RabbitMQ is running on port 5672
3. No port conflicts (5000, 5001, 5002, 5003, 5004, 5010)

**Solution:**
- Start MySQL: `net start MySQL80` (Windows) or check services
- Start RabbitMQ: Check RabbitMQ service is running
- Kill conflicting processes: `Get-Process | Where-Object {$_.ProcessName -like '*dotnet*'} | Stop-Process`

### 401 Unauthorized Errors

**Cause:** Token expired or invalid

**Solution:**
1. Run "Login" request again
2. Token will be refreshed automatically
3. Retry failed request

### 500 Internal Server Errors

**Cause:** Database not available or migration not applied

**Solution:**
1. Check service logs in PowerShell window
2. Ensure MySQL databases exist:
   - `GymHiveAuth`
   - `GymHiveGyms`
   - `GymHiveMemberships`
   - `GymHiveNotifications`
   - `GymHiveWorkoutLogs`
3. Services will auto-migrate on startup

### Check-In Fails with "Already checked in"

**Cause:** Previous check-in not completed

**Solution:**
1. Get workout logs: "Get My Workout Logs"
2. Find active check-in (CheckOutTime = null)
3. Check out: "Check Out from Gym" with that ID

### Gateway Cannot Reach Services

**Cause:** Services not fully started or health checks failing

**Solution:**
1. Wait 60 seconds after startup
2. Check individual service Swagger pages
3. Verify gateway health: http://localhost:5000/health

---

## 📈 Performance Testing

The gateway supports high-scale testing. For load testing:

1. Use multiple Postman runners in parallel
2. Test with different user accounts
3. Monitor Prometheus metrics at:
   - Gateway: http://localhost:5000/metrics
   - Services: http://localhost:500X/metrics

---

## 🎯 Key Test Scenarios

### Scenario 1: New Member Registration
1. Register Member
2. Get All Gyms
3. Purchase Membership (if membership plans exist)

### Scenario 2: Daily Gym Visit
1. Login
2. Check In to Gym
3. (Wait or do other actions)
4. Check Out from Gym
5. View Workout Statistics

### Scenario 3: Notification Management
1. Perform actions that trigger notifications (check-in, check-out, etc.)
2. Get My Notifications
3. Mark notifications as read
4. Get Unread Count (should be 0)

### Scenario 4: Multi-Service Integration
1. Check In (WorkoutLoggingService)
2. Verify notification created (NotificationsService)
3. Check membership status (MembershipService)
4. View gym details (GymService)

---

## 📝 Notes

- **Auto-Migration**: Services automatically create/update database schemas on startup
- **Event-Driven**: Check-in and check-out trigger RabbitMQ events that create notifications
- **Token Auto-Save**: Postman collection automatically saves and reuses authentication tokens
- **Variable Auto-Population**: Important IDs (user_id, workout_log_id) are automatically captured from responses

---

## 🆘 Support

For issues or questions:
1. Check service logs in PowerShell windows
2. Verify database connections
3. Check RabbitMQ management UI: http://localhost:15672 (guest/guest)
4. Review service Swagger documentation

---

## ✅ Success Criteria

All tests pass when:
- ✅ Gateway health returns all services as healthy
- ✅ Login returns valid JWT token
- ✅ Check-in creates workout log and notification
- ✅ Check-out calculates duration correctly
- ✅ Statistics show accurate workout data
- ✅ Notifications are delivered and can be marked as read

---

**Happy Testing! 🚀**
