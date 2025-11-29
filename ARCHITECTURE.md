# GymHive Microservices Architecture (C2 Container Diagram)

## User Roles
- 👤 **Gym Member** - Regular users who join gyms, book classes, post in feed
- 👨‍💼 **Moderator** - Gym staff who manage classes, bookings, and gym groups
- 👨‍💻 **Admin** - System administrators who create gyms and assign moderators

*(No Coach/Instructor role - simplified to Member and Moderator)*

---

## Current Services (Implemented)

### 1. **Identity & Access Service (Authentication)** (Port 5010)
- **Database**: MySQL (EF Core)
- **Responsibility**: User authentication, JWT token generation, user management, role-based access
- **Events Published**: `UserRegisteredEvent`
- **Key Endpoints**:
  - POST `/api/auth/register` - User registration (creates Member role by default)
  - POST `/api/auth/login` - User login with JWT token
  - GET `/api/auth/users` - List users (Admin only)
  - PUT `/api/auth/users/{id}/role` - Update user role: Member, Moderator, Admin (Admin only)
  - GET `/api/auth/me` - Get current user profile

### 2. **Gyms & Groups Service** (Port 5001)
- **Database**: MySQL (EF Core)
- **Responsibility**: Gym management, gym groups for member discussions, moderator assignments (full CRUD)
- **Events Published**: `GymCreatedEvent`, `GymGroupCreatedEvent`, `ModeratorAssignedEvent`, `ModeratorRemovedEvent`
- **Key Endpoints**:
  - **Gyms** (Admin only):
    - GET/POST/PUT/DELETE `/api/gyms` - CRUD operations for gyms
    - GET `/api/gyms/{gymId}/moderators` - Get list of gym moderators
    - POST `/api/gyms/{gymId}/moderators` - Assign moderator to gym
    - PUT `/api/gyms/{gymId}/moderators/{userId}` - Update moderator permissions/role
    - DELETE `/api/gyms/{gymId}/moderators/{userId}` - Remove moderator from gym
  - **Gym Groups** (Moderators):
    - GET/POST/PUT/DELETE `/api/gymgroups` - Create discussion groups within gyms
    - POST `/api/gymgroups/{groupId}/members` - Add members to groups
    - GET `/api/gymgroups/{groupId}/posts` - Group posts/discussions
  - **Members**:
    - GET `/api/gyms` - List available gyms
    - GET `/api/gyms/{gymId}/groups` - View gym groups they can join

### 3. **Membership Service** (Port 5002)
- **Database**: MongoDB
- **Responsibility**: Membership purchases, membership tracking, gym access management
- **Events Published**: `MembershipPurchasedEvent`, `MembershipExpiredEvent`
- **Key Endpoints**:
  - GET `/api/memberships/my-memberships` - User's active/past memberships
  - POST `/api/memberships` - Purchase gym membership
  - GET `/api/memberships/gym/{gymId}` - List gym members (Moderator/Admin)
  - GET `/api/memberships/{id}/status` - Check membership status

---

## Planned Services

### 4. **Social Network Service** 🌐 (Port 5008) **[Graph Database - Neo4j]**
- **Database**: Neo4j (Graph Database)
- **Responsibility**: Friend connections, friend recommendations, direct messaging (1-on-1 and group chats), workout buddy matching
- **Events Published**: `FriendRequestSentEvent`, `FriendRequestAcceptedEvent`, `MessageSentEvent`
- **Events Subscribed**: `UserRegisteredEvent` (create user node), `WorkoutLoggedEvent` (update activity graph)

**Why Neo4j?**
- Ideal for modeling social relationships (FRIEND_OF, TRAINS_WITH, MESSAGES)
- Fast traversal for friend recommendations ("friends of friends who train at same gym")
- Pattern matching for workout buddy suggestions
- Efficient mutual friend queries

**Database Schema (Graph Model)**:
```cypher
// Nodes
(User {id: guid, username: string, fullName: string, profileImageUrl: string, createdAt: datetime})
(Gym {id: guid, name: string})
(Message {id: guid, content: string, sentAt: datetime})
(Conversation {id: guid, type: string, createdAt: datetime}) // type: "direct", "group"

// Relationships
(User)-[:FRIEND_OF {since: datetime, status: string}]->(User) // status: "pending", "accepted", "blocked"
(User)-[:TRAINS_AT {memberSince: datetime}]->(Gym)
(User)-[:SENT {sentAt: datetime}]->(Message)
(Message)-[:IN_CONVERSATION]->(Conversation)
(User)-[:PARTICIPANT_IN]->(Conversation)
(User)-[:TRAINS_WITH {frequency: int}]->(User) // derived from workout logs
```

**Endpoints**:

**Friend Management**:
- **POST `/api/friends/request`** - Send friend request
  - Purpose: User sends friend connection request
  - Body: `{ targetUserId }`
  - Creates: `(User)-[:FRIEND_OF {status: "pending"}]->(TargetUser)`
  - Auth: Required
  - Publishes: `FriendRequestSentEvent`
  
- **PUT `/api/friends/request/{requestId}/accept`** - Accept friend request
  - Purpose: User accepts pending request
  - Updates: Changes status to "accepted", creates bidirectional relationship
  - Auth: Required (recipient only)
  - Publishes: `FriendRequestAcceptedEvent`
  
- **DELETE `/api/friends/{userId}`** - Remove friend
  - Purpose: Unfriend user
  - Deletes: FRIEND_OF relationship (both directions)
  - Auth: Required
  
- **GET `/api/friends`** - Get user's friends list
  - Purpose: Display friends page
  - Query: `MATCH (me:User {id: $userId})-[:FRIEND_OF {status: "accepted"}]-(friend:User) RETURN friend`
  - Response: `[{ userId, username, fullName, profileImageUrl, friendSince, mutualFriendsCount }]`
  - Auth: Required
  
- **GET `/api/friends/requests`** - Get pending friend requests
  - Purpose: Show friend requests inbox
  - Query: `MATCH (sender:User)-[:FRIEND_OF {status: "pending"}]->(me:User {id: $userId})`
  - Response: `[{ requestId, userId, username, fullName, sentAt, mutualFriendsCount }]`
  - Auth: Required

**Friend Recommendations**:
- **GET `/api/friends/recommendations`** - Get friend suggestions
  - Purpose: "People you may know" feature
  - Algorithm: Friends of friends, same gym members, similar workout patterns
  - Cypher Query:
    ```cypher
    // Friends of friends
    MATCH (me:User {id: $userId})-[:FRIEND_OF {status: "accepted"}]-(friend)-[:FRIEND_OF {status: "accepted"}]-(suggestion:User)
    WHERE NOT (me)-[:FRIEND_OF]-(suggestion) AND suggestion.id <> $userId
    WITH suggestion, COUNT(friend) as mutualFriends
    
    // Same gym members
    OPTIONAL MATCH (me)-[:TRAINS_AT]->(gym:Gym)<-[:TRAINS_AT]-(suggestion)
    
    RETURN suggestion, mutualFriends, COLLECT(gym.name) as sharedGyms
    ORDER BY mutualFriends DESC, SIZE(sharedGyms) DESC
    LIMIT 20
    ```
  - Response: `[{ userId, username, fullName, mutualFriendsCount, sharedGyms: ["Gym A", "Gym B"] }]`
  - Auth: Required
  
- **GET `/api/friends/mutual/{userId}`** - Get mutual friends
  - Purpose: Show shared connections with another user
  - Query: `MATCH (me)-[:FRIEND_OF {status: "accepted"}]-(mutual)-[:FRIEND_OF {status: "accepted"}]-(them:User {id: $targetUserId})`
  - Auth: Required

**Workout Buddy Matching**:
- **GET `/api/friends/workout-buddies`** - Find workout partners
  - Purpose: Match users who train at same gym at similar times
  - Query: Analyzes TRAINS_AT relationships + workout log timestamps
  - Response: `[{ userId, username, commonGym, overlapScore, lastSeenAt }]`
  - Auth: Required

**Messaging System**:
- **POST `/api/messages`** - Send message
  - Purpose: Send direct or group message
  - Body: `{ conversationId?, recipientUserIds: [guid], content, messageType: "text|image|video" }`
  - Creates: Message node → links to Conversation
  - Auth: Required
  - Publishes: `MessageSentEvent` (for push notifications)
  
- **GET `/api/messages/conversations`** - Get user's conversations
  - Purpose: Chat list (like WhatsApp/Messenger)
  - Query: `MATCH (me:User {id: $userId})-[:PARTICIPANT_IN]->(conv:Conversation)`
  - Response: `[{ conversationId, type, participants: [{userId, username}], lastMessage, unreadCount, updatedAt }]`
  - Auth: Required
  
- **GET `/api/messages/conversation/{conversationId}`** - Get conversation messages
  - Purpose: Load message thread
  - Query: `MATCH (msg:Message)-[:IN_CONVERSATION]->(conv:Conversation {id: $convId})`
  - Response: `[{ messageId, senderId, content, sentAt, readBy: [userId] }]`
  - Pagination: Skip/limit for infinite scroll
  - Auth: Required (participant only)
  
- **PUT `/api/messages/{messageId}/read`** - Mark message as read
  - Purpose: Update read receipts
  - Auth: Required
  
- **POST `/api/messages/conversations/group`** - Create group chat
  - Purpose: Multi-person conversation (gym groups, workout teams)
  - Body: `{ name, participantUserIds: [guid] }`
  - Creates: Conversation node with PARTICIPANT_IN relationships
  - Auth: Required

**Business Logic**:
- Friend requests expire after 30 days if not accepted
- Users can block others (creates BLOCKED relationship, hides from recommendations)
- Message retention: 1 year for direct messages, indefinite for group chats
- Read receipts: Track which users have read each message
- Typing indicators: Real-time via SignalR (not stored in Neo4j)
- Friend limit: 1000 connections per user (prevent spam)

**Integration with Other Services**:
- **Identity Service**: Syncs user creation (`UserRegisteredEvent` → create User node)
- **Workout Logging**: Updates TRAINS_WITH relationship weights based on workout overlap
- **Notifications Service**: Subscribes to `MessageSentEvent`, `FriendRequestSentEvent` for push notifications
- **Social Feed**: Friends' posts appear in feed (queries friend list from Neo4j)

---

### 5. **Notifications Service** (Port 5003)
- **Database**: MySQL (EF Core)
- **Responsibility**: In-app notification system (NO email/SMS - app notifications only)
- **Events Subscribed**: ALL events (UserRegistered, MembershipPurchased, ClassBooked, WorkoutLogged, PostCreated, etc.)
- **Events Published**: None (consumer service)

**Database Schema**:
```sql
Notifications:
  - Id (int, PK)
  - UserId (Guid) - recipient
  - Type (string) - enum: MembershipPurchased, ClassBooked, WorkoutReminder, NewPost, GroupInvite
  - Title (string)
  - Message (string)
  - RelatedEntityId (string) - gym ID, class ID, post ID, etc.
  - RelatedEntityType (string) - "gym", "class", "post", "booking"
  - IsRead (bool)
  - CreatedAt (DateTime)
```

**Endpoints**:
- **GET `/api/notifications`** - Get user's in-app notifications (paginated, newest first)
  - Purpose: Display notification bell dropdown in UI
  - Response: `[{ id, type, title, message, isRead, createdAt, relatedEntityId }]`
  - Auth: Required (own notifications only)
  
- **GET `/api/notifications/unread-count`** - Get count of unread notifications
  - Purpose: Badge count on notification icon
  - Response: `{ count: 5 }`
  - Auth: Required
  
- **PUT `/api/notifications/{id}/read`** - Mark notification as read
  - Purpose: User clicks notification
  - Auth: Required (own notifications only)
  
- **PUT `/api/notifications/read-all`** - Mark all as read
  - Purpose: "Mark all as read" button
  - Auth: Required
  
- **DELETE `/api/notifications/{id}`** - Delete notification
  - Purpose: Dismiss notification
  - Auth: Required (own notifications only)

**Event Subscription Logic (In-App Notifications Only)**:
- `UserRegisteredEvent` → "Welcome to GymHive! 🎉"
- `MembershipPurchasedEvent` → To User: "Your {gymName} membership is now active!", To Moderators: "New member joined {gymName}"
- `ClassBookedEvent` → "You're booked for {className} at {time}"
- `ClassReminderEvent` → "Your class starts in 30 minutes" (generated by scheduler)
- `WorkoutLoggedEvent` → To followers: "{userName} checked in at {gymName}"
- `PostCreatedEvent` → To gym members: "{gymName} posted an announcement"
- `GroupInviteEvent` → "You've been invited to join {groupName}"

---

### 6. **Workout Logging Service** (Port 5004)
- **Database**: MySQL (EF Core)
- **Responsibility**: Track gym member check-ins to scheduled classes/timeslots
- **Events Published**: `WorkoutLoggedEvent` (when member clocks in)
- **Events Subscribed**: `ClassBookedEvent` (to validate booking exists)

**Database Schema**:
```sql
WorkoutLogs:
  - Id (int, PK)
  - UserId (Guid, FK) - gym member
  - GymId (int, FK)
  - BookingId (int, FK, nullable) - reference to booking if they booked a class
  - CheckInTime (DateTime)
  - CheckOutTime (DateTime, nullable) - optional clock out
  - Duration (int, nullable) - minutes, calculated from checkout
  - Notes (string, nullable) - member's workout notes
  - CreatedAt (DateTime)
```

**Endpoints**:
- **POST `/api/workouts/checkin`** - Clock in to gym/class
  - Purpose: Member arrives at gym and checks in (with or without booking)
  - Body: `{ gymId, bookingId?, notes? }`
  - Auth: Required (Member role + active membership)
  - Validation: Must have active membership for gym
  - Response: `{ workoutLogId, checkInTime, gymName }`
  - Publishes: `WorkoutLoggedEvent`
  
- **PUT `/api/workouts/{id}/checkout`** - Clock out from gym
  - Purpose: Member leaves gym, records workout duration
  - Body: `{ notes? }` (optional workout notes)
  - Auth: Required (owner only)
  - Calculates duration automatically
  
- **GET `/api/workouts/my-logs`** - Get user's workout history
  - Purpose: Personal workout tracking, statistics
  - Query: `startDate`, `endDate`, `gymId` (optional filters)
  - Response: List of check-ins with durations
  - Auth: Required (own logs only)
  
- **GET `/api/workouts/gym/{gymId}/attendance`** - Get gym attendance (Moderators)
  - Purpose: See which members are currently at gym, daily attendance stats
  - Query: `date` (default today)
  - Auth: Required (Moderator of gym or Admin)
  - Response: `{ currentlyAtGym: [...], totalToday: 42, averageTime: 65 minutes }`
  
- **GET `/api/workouts/stats`** - Get user's workout statistics
  - Purpose: Dashboard stats (total workouts, avg duration, favorite gym)
  - Auth: Required
  - Response: `{ totalWorkouts, totalMinutes, avgDuration, favoriteGym, thisWeek, thisMonth }`

**Business Logic**:
- Only gym members with active memberships can check in
- If bookingId provided, validates booking exists and belongs to user
- Can check in without booking (free gym time)
- One active check-in per user (must checkout before new checkin)
- Moderators can view real-time gym occupancy

---

### 7. **Classes & Booking Service** (Port 5005)
- **Database**: MySQL (EF Core)
- **Responsibility**: Class scheduling by moderators, booking management for members
- **Events Published**: `ClassCreatedEvent`, `ClassBookedEvent`, `ClassCancelledEvent`
- **Events Subscribed**: `MembershipPurchasedEvent` (enable booking)

**Database Schema**:
```sql
Classes:
  - Id (int, PK)
  - GymId (int, FK)
  - Name (string) - "Yoga", "CrossFit", "Spin Class"
  - Description (string)
  - Capacity (int) - max participants
  - CreatedBy (Guid, FK) - moderator who created it
  - IsActive (bool)
  
ClassSchedules:
  - Id (int, PK)
  - ClassId (int, FK)
  - StartTime (DateTime)
  - EndTime (DateTime)
  - RecurrenceRule (string, nullable) - "Weekly Monday 18:00" for recurring classes
  - Status (enum) - Scheduled, InProgress, Completed, Cancelled
  
Bookings:
  - Id (int, PK)
  - UserId (Guid, FK) - member
  - ScheduleId (int, FK)
  - BookedAt (DateTime)
  - Status (enum) - Confirmed, Cancelled, Attended, NoShow
  - CancellationReason (string, nullable)
  - CreatedAt (DateTime)
  
Waitlist:
  - Id (int, PK)
  - UserId (Guid, FK)
  - ScheduleId (int, FK)
  - Position (int) - queue position
  - CreatedAt (DateTime)
```

**Endpoints**:

**Class Management (Moderators)**:
- **POST `/api/classes`** - Create new class type
  - Purpose: Moderator defines class (e.g., "Morning Yoga")
  - Body: `{ gymId, name, description, capacity }`
  - Auth: Required (Moderator of gym or Admin)
  - Publishes: `ClassCreatedEvent`
  
- **GET `/api/classes/gym/{gymId}`** - List gym's classes
  - Purpose: Show available class types
  - Auth: Optional (public)
  
- **PUT `/api/classes/{id}`** - Update class details
  - Auth: Required (Moderator or Admin)
  
- **DELETE `/api/classes/{id}`** - Deactivate class
  - Auth: Required (Moderator or Admin)

**Class Scheduling (Moderators)**:
- **POST `/api/classes/{classId}/schedules`** - Schedule class timeslot
  - Purpose: Moderator creates specific time slot for class
  - Body: `{ startTime, endTime, recurrenceRule? }`
  - Example: Schedule "Morning Yoga" every Monday at 6 AM
  - Auth: Required (Moderator or Admin)
  
- **GET `/api/classes/schedules`** - Get upcoming class schedules
  - Purpose: Calendar view of all scheduled classes
  - Query: `gymId`, `startDate`, `endDate`, `classId`
  - Response: `[{ scheduleId, className, startTime, endTime, capacity, bookedCount, waitlistCount }]`
  - Auth: Optional (public)
  
- **PUT `/api/classes/schedules/{id}`** - Reschedule or cancel class
  - Body: `{ startTime?, endTime?, status? }`
  - Auth: Required (Moderator or Admin)

**Booking (Members)**:
- **POST `/api/bookings`** - Book a class
  - Purpose: Member reserves spot in class
  - Body: `{ scheduleId }`
  - Auth: Required (Member with active membership)
  - Validation: Not full, member has membership, no conflicting bookings
  - If full → add to waitlist
  - Publishes: `ClassBookedEvent`
  
- **GET `/api/bookings/my-bookings`** - Get user's bookings
  - Purpose: Show upcoming classes user booked
  - Query: `status` (optional filter: upcoming, past, cancelled)
  - Auth: Required
  
- **DELETE `/api/bookings/{id}`** - Cancel booking
  - Purpose: Member cancels reservation
  - Body: `{ reason? }`
  - Auth: Required (owner only, or Moderator)
  - If cancellation → promote waitlist user to booking
  
- **GET `/api/classes/schedules/{scheduleId}/attendees`** - Get class attendees
  - Purpose: See who's booked (for moderators) or how full class is (for members)
  - Auth: Members see count only, Moderators see full list
  
- **GET `/api/bookings/waitlist/{scheduleId}`** - Get waitlist position
  - Purpose: Check queue status
  - Auth: Required

**Business Logic**:
- Only gym members with active memberships can book classes
- Capacity enforced: once full, users added to waitlist
- Waitlist automatically promoted when bookings cancelled
- Members can cancel up to 2 hours before class (configurable)
- No-show tracking: if member booked but didn't check in (via Workout Logging)
- Recurring classes: generate schedules for next 2 weeks automatically

---

### 8. **Social Feed Service** (Port 5006)
- **Database**: **NoSQL (MongoDB)** - for fast feed queries and flexible post schema
- **Responsibility**: Posts, announcements, main feed, moderation
- **Events Published**: `PostCreatedEvent`, `PostLikedEvent`, `CommentPostedEvent`
- **Events Subscribed**: `UserRegisteredEvent`, `GymCreatedEvent` (create feed entities)

**Why MongoDB for Feed?**
- **Fast reads**: Feed queries are read-heavy, MongoDB's document model optimized for this
- **Flexible schema**: Posts can have various content types (text, images, videos, polls)
- **Denormalization**: Store author info directly in post document for fast feed rendering
- **No complex joins**: Unlike relational DBs, posts pre-loaded with user/gym data

**EF Core MongoDB Support?**
- ✅ **Yes! EF Core has MongoDB provider**: `MongoDB.EntityFrameworkCore`
- Use familiar EF Core patterns (DbContext, DbSet, LINQ)
- Connection string: `mongodb://localhost:27017`
- Similar to current MembershipService implementation

**Database Schema** (MongoDB Collections):

```csharp
Posts:
  - Id (string, MongoDB ObjectId)
  - AuthorId (Guid) - user or gym
  - AuthorType (string) - "User" or "Gym" (gyms can post announcements)
  - AuthorName (string) - denormalized for fast display
  - AuthorAvatar (string) - denormalized
  - Content (string) - post text
  - MediaUrls (string[]) - array of image/video URLs
  - GymId (int, nullable) - if posted about specific gym
  - GymName (string, nullable) - denormalized
  - Likes (string[]) - array of UserIds who liked
  - LikeCount (int) - cached count
  - CommentCount (int) - cached count
  - IsAnnouncement (bool) - if posted by gym as announcement
  - IsPinned (bool) - gym mods can pin important posts
  - CreatedAt (DateTime)
  - UpdatedAt (DateTime, nullable)
  
Comments:
  - Id (string, MongoDB ObjectId)
  - PostId (string, FK)
  - AuthorId (Guid)
  - AuthorName (string) - denormalized
  - AuthorAvatar (string)
  - Content (string)
  - Likes (string[]) - comments can be liked too
  - CreatedAt (DateTime)
```

**Endpoints**:

**Post Management**:
- **POST `/api/feed/posts`** - Create post
  - Purpose: Member or gym shares content
  - Body: `{ content, mediaUrls?, gymId?, isAnnouncement? }`
  - Auth: Required (Members or Moderators)
  - Validation: If isAnnouncement=true, must be Moderator
  - Publishes: `PostCreatedEvent`
  
- **GET `/api/feed`** - Get main feed (homepage)
  - Purpose: Show posts from user's gyms + followed users (if social features added later)
  - Algorithm: 
    1. Posts from gyms user is member of
    2. Sorted by: pinned first, then by recency and engagement (likes/comments)
  - Query: `page`, `pageSize`, `gymId?` (filter by gym)
  - Auth: Required (Members)
  - Response: `[{ postId, author, content, mediaUrls, likes, comments, createdAt }]`
  
- **GET `/api/feed/posts/{postId}`** - Get single post with comments
  - Purpose: Post detail page
  - Auth: Optional (public if gym posts, restricted to members otherwise)
  
- **PUT `/api/feed/posts/{postId}`** - Edit post
  - Purpose: Update content
  - Body: `{ content, mediaUrls? }`
  - Auth: Required (owner or Moderator)
  
- **DELETE `/api/feed/posts/{postId}`** - Delete post
  - Purpose: Remove post
  - Auth: Required (owner, gym Moderator, or Admin)
  
- **PUT `/api/feed/posts/{postId}/pin`** - Pin/unpin post (Moderators only)
  - Purpose: Highlight important gym announcements
  - Body: `{ isPinned: true }`
  - Auth: Required (Moderator of gym or Admin)

**Engagement**:
- **POST `/api/feed/posts/{postId}/like`** - Like post
  - Purpose: Express approval
  - Auth: Required
  - Updates: Add userId to Likes array, increment LikeCount
  
- **DELETE `/api/feed/posts/{postId}/like`** - Unlike post
  - Auth: Required
  
- **POST `/api/feed/posts/{postId}/comments`** - Comment on post
  - Purpose: Discussion
  - Body: `{ content }`
  - Auth: Required
  - Creates new Comment document, increments post.CommentCount
  - Publishes: `CommentPostedEvent` (triggers notification)
  
- **GET `/api/feed/posts/{postId}/comments`** - Get post comments
  - Purpose: Display comment thread
  - Query: `page`, `pageSize`
  - Auth: Optional (if post is public)
  
- **DELETE `/api/feed/comments/{commentId}`** - Delete comment
  - Auth: Required (owner, post author, or Moderator)

**Business Logic**:
- Gym announcements (isAnnouncement=true) visible to all gym members in feed
- Regular posts from members visible to same gym community
- Pinned posts always show at top of gym's feed
- Moderators can delete any posts in their gym
- Denormalized author data updated on user profile changes (eventual consistency via events)

---

### 9. **Media Service** (Port 5007)
- **Database**: **Cloud Blob Storage** (Azure Blob / AWS S3) + MySQL metadata
- **Responsibility**: Image/video upload, storage, QR code generation for gym check-ins
- **Events Published**: `MediaUploadedEvent`
- **Events Subscribed**: None (stateless upload service)

**Why Blob Storage + MySQL?**
- **Blob Storage**: Store actual files (images, videos) in cloud for scalability
- **MySQL**: Store metadata (URL, owner, size, type) for querying and access control
- **EF Core**: Only for metadata, blob storage via SDK (Azure.Storage.Blobs / AWSSDK.S3)

**Database Schema** (MySQL):
```sql
MediaFiles:
  - Id (int, PK)
  - UploadedBy (Guid, FK) - user who uploaded
  - FileName (string)
  - FileSize (long) - bytes
  - ContentType (string) - image/jpeg, video/mp4
  - BlobUrl (string) - full URL to blob storage
  - ThumbnailUrl (string, nullable) - for videos
  - EntityType (string) - "Post", "Profile", "Gym", "Class"
  - EntityId (string) - related entity ID
  - IsPublic (bool) - access control
  - UploadedAt (DateTime)

QRCodes:
  - Id (int, PK)
  - GymId (int, FK)
  - QRCodeUrl (string) - URL to QR code image
  - Purpose (string) - "CheckIn", "GymInfo"
  - ExpiresAt (DateTime, nullable) - for temporary QR codes
  - CreatedAt (DateTime)
```

**Endpoints**:

**Image/Video Upload**:
- **POST `/api/media/upload`** - Upload image or video
  - Purpose: Upload files for posts, profiles, gym photos
  - Body: Multipart form data with file + metadata
  - Query: `entityType` (Post, Profile, Gym), `entityId?`
  - Auth: Required
  - Process:
    1. Validate file (size limit, type)
    2. Upload to blob storage (Azure/S3)
    3. Store metadata in MySQL
    4. For videos: generate thumbnail
  - Response: `{ mediaId, url, thumbnailUrl? }`
  - Publishes: `MediaUploadedEvent`
  
- **GET `/api/media/{id}`** - Get media file (redirect to blob URL)
  - Purpose: Serve media with access control
  - Auth: Optional (if public) or Required (if private)
  - Returns: Redirect to blob storage URL or signed URL
  
- **DELETE `/api/media/{id}`** - Delete media file
  - Purpose: Remove uploaded file
  - Auth: Required (owner, Moderator, or Admin)
  - Process: Delete from blob storage + database

**QR Code Generation**:
- **POST `/api/media/qr-codes/gym/{gymId}`** - Generate gym QR code
  - Purpose: Gyms can generate QR codes for easy check-in
  - Body: `{ purpose: "CheckIn" }`
  - Auth: Required (Moderator of gym or Admin)
  - Process:
    1. Generate QR code with gym check-in URL
    2. Upload QR image to blob storage
    3. Store in database
  - Response: `{ qrCodeUrl, downloadUrl }`
  
- **GET `/api/media/qr-codes/gym/{gymId}`** - Get gym's QR codes
  - Purpose: Moderators view/download QR codes
  - Auth: Required (Moderator or Admin)

**Image Processing**:
- Resize images (thumbnails, profile pics)
- Video thumbnail extraction
- Image compression for faster loading
- Supported formats: JPEG, PNG, GIF, MP4, MOV

**Storage Configuration**:
- Local development: Store in `wwwroot/uploads` (temp)
- Production: Azure Blob Storage or AWS S3
- CDN integration for fast global delivery
- Signed URLs for private content (expire after 1 hour)

---

## Event Flow Diagram

```
UserRegisters
    ↓
[IdentityService] --UserRegisteredEvent--> [RabbitMQ]
                                             ↓
                                    [NotificationsService]
                                    (Welcome notification)

MembershipPurchased
    ↓
[MembershipService] --MembershipPurchasedEvent--> [RabbitMQ]
                                                    ↓
                                    ┌──────────────┴──────────────┐
                                    ↓                             ↓
                            [NotificationsService]      [ClassesBookingService]
                            (Confirm to user,           (Enable class booking)
                             notify mods)

ClassBooked
    ↓
[ClassesBookingService] --ClassBookedEvent--> [RabbitMQ]
                                                ↓
                                        [NotificationsService]
                                        (Booking confirmation)

WorkoutLogged (CheckIn)
    ↓
[WorkoutLoggingService] --WorkoutLoggedEvent--> [RabbitMQ]
                                                  ↓
                                        [NotificationsService]
                                        (Achievement notifications)

PostCreated
    ↓
[SocialFeedService] --PostCreatedEvent--> [RabbitMQ]
                                            ↓
                                    [NotificationsService]
                                    (Notify gym members if announcement)

MediaUploaded
    ↓
[MediaService] --MediaUploadedEvent--> [RabbitMQ]
                                         ↓
                                    (Logged for analytics)
```

---

## Technology Stack Summary

| Service | Database | Tech | Port | Events Published |
|---------|----------|------|------|------------------|
| **Identity & Access** | MySQL | EF Core | 5010 | UserRegisteredEvent |
| **Gyms & Groups** | MySQL | EF Core | 5001 | GymCreatedEvent, GymGroupCreatedEvent |
| **Membership** | MongoDB | EF Core (MongoDB Provider) | 5002 | MembershipPurchasedEvent |
| **Social Network** 🌐 | **Neo4j** | **Neo4j.Driver (Cypher)** | **5008** | **FriendRequestSentEvent, MessageSentEvent** |
| **Notifications** | MySQL | EF Core | 5003 | None (event consumer) |
| **Workout Logging** | MySQL | EF Core | 5004 | WorkoutLoggedEvent |
| **Classes & Booking** | MySQL | EF Core | 5005 | ClassCreatedEvent, ClassBookedEvent |
| **Social Feed** | MongoDB | EF Core (MongoDB Provider) | 5006 | PostCreatedEvent, CommentPostedEvent |
| **Media** | Azure Blob + MySQL | Azure.Storage.Blobs + EF Core | 5007 | MediaUploadedEvent |

---

## Implementation Priority & Complexity

### Phase 1 - Core Social & Booking (High Priority)
1. **Social Network Service** 🔴 Complex **[Graph Database Showcase]**
   - Neo4j graph database (demonstrates advanced database knowledge)
   - Friend connections with recommendations algorithm
   - Real-time messaging system
   - Foundation for social features (workout buddies, feed)
   - **Critical**: Must be implemented early as Social Feed depends on friend graph
   
2. **Classes & Booking Service** 🟡 Medium
   - Critical for gym operations
   - Moderators need this to schedule classes
   - Members need this to book slots
   - Foundation for workout tracking
   
3. **Workout Logging Service** 🟢 Easy
   - Tracks gym attendance
   - Links to bookings
   - Simple CRUD with check-in/out logic

### Phase 2 - Communication & Content (Medium Priority)
4. **Notifications Service** 🟢 Easy
   - In-app only, no email/SMS complexity
   - Event consumer pattern (similar to existing publishers)
   - Essential for user engagement
   - **Depends on**: Social Network Service (for friend request/message notifications)
   
5. **Social Feed Service** 🟡 Medium
   - MongoDB (like MembershipService)
   - Feed algorithm with sorting
   - Gym announcements + member posts
   - **Depends on**: Social Network Service (queries friend list for personalized feed)

### Phase 3 - Media & Enhancement (Lower Priority)
6. **Media Service** 🔴 Complex
   - Blob storage integration (Azure/AWS)
   - Image processing (resize, thumbnails)
   - QR code generation
   - Can start with local file storage, migrate to cloud later
   - Used by Social Feed (post images) and Social Network (profile pictures, message attachments)

---

## Development Roadmap

### Step 1: Update Event Library
**File**: `GymHive.Messaging/Events/`
- Add new event classes:
  - `FriendRequestSentEvent` (userId, targetUserId, sentAt)
  - `FriendRequestAcceptedEvent` (userId, friendId, acceptedAt)
  - `MessageSentEvent` (messageId, senderId, recipientIds, conversationId, sentAt)
  - `ClassCreatedEvent`
  - `ClassBookedEvent`
  - `ClassCancelledEvent`
  - `WorkoutLoggedEvent`
  - `PostCreatedEvent`
  - `CommentPostedEvent`
  - `MediaUploadedEvent`

### Step 2: Scaffold Services
Follow existing pattern (AuthenticationService/GymService/MembershipService):
```
ServiceName/
├── Program.cs (with RabbitMQ setup)
├── appsettings.json
├── ServiceNameController.cs
├── BLL/
│   ├── Managers/
│   ├── ManagerInterfaces/
│   ├── DTOs/
│   └── Entities/
├── DAL/
│   ├── DbContexts/
│   ├── Repositories/
│   └── RepositoryInterfaces/
└── Tests/
```

### Step 3: API Gateway Updates
**File**: `ApiGateway/appsettings.json`
Add YARP routes:
- `/api/friends/**` → SocialNetworkService:5008
- `/api/messages/**` → SocialNetworkService:5008
- `/api/notifications/**` → NotificationsService:5003
- `/api/workouts/**` → WorkoutLoggingService:5004
- `/api/classes/**` → ClassesBookingService:5005
- `/api/bookings/**` → ClassesBookingService:5005
- `/api/feed/**` → SocialFeedService:5006
- `/api/media/**` → MediaService:5007

### Step 4: Docker Compose
**File**: `docker-compose.yml`
Add new service containers + databases:
- `social-network-service` + `neo4j-db` (Neo4j with APOC plugin, ports 7474/7687)
- `notifications-service` + `notifications-db` (MySQL)
- `workout-logging-service` + `workout-db` (MySQL)
- `classes-booking-service` + `classes-db` (MySQL)
- `social-feed-service` + `feed-db` (MongoDB)
- `media-service` + `media-db` (MySQL) + blob volume

**Neo4j Configuration**:
```yaml
neo4j-db:
  image: neo4j:5.15-community
  environment:
    NEO4J_AUTH: neo4j/Neo4jPassword123!
    NEO4J_PLUGINS: '["apoc"]'
  ports:
    - "7474:7474"  # Browser UI
    - "7687:7687"  # Bolt protocol
  volumes:
    - neo4j-data:/data
```

### Step 5: Frontend Integration
Add new pages/components:
- `FriendsList.svelte` (connections page with friend requests)
- `FriendRecommendations.svelte` (people you may know)
- `MessagingPanel.svelte` (chat interface like Messenger/WhatsApp)
- `ChatWindow.svelte` (message thread with real-time updates)
- `NotificationBell.svelte` (header dropdown)
- `ClassesSchedule.svelte` (calendar view)
- `BookClass.svelte` (booking modal)
- `WorkoutHistory.svelte` (personal stats)
- `Feed.svelte` (main feed page - shows friends' posts)
- `CreatePost.svelte` (post composer)

**Real-time Features**:
- SignalR Hub for messaging (live message delivery, typing indicators)
- Friend request notifications (toast/badge updates)
- Message unread count in header

---

## Key Architectural Decisions

✅ **No Coach Role**: Simplified to Member, Moderator, Admin
✅ **No Email Notifications**: In-app notifications only
✅ **Workout Logging ≠ Review**: Separate concerns (attendance tracking vs. gym ratings)
✅ **MongoDB for Feed**: Fast reads, flexible schema for posts
✅ **Neo4j for Social Graph**: Graph database for friend connections, recommendations, messaging
✅ **Blob Storage for Media**: Scalable file storage (Azure Blob / AWS S3)
✅ **Event-Driven**: All services publish events for loose coupling
✅ **Database Per Service**: Maintains microservice independence
✅ **Real-time Messaging**: SignalR for live chat, typing indicators, presence

**Technology Showcase**:
- 🔵 **Relational (MySQL)**: Identity, Gyms, Notifications, Workout, Classes, Media metadata
- 🟢 **Document (MongoDB)**: Membership, Social Feed (flexible schemas, fast reads)
- 🔴 **Graph (Neo4j)**: Social Network (relationship modeling, graph algorithms)
- ☁️ **Blob Storage (Azure)**: Media files (images, videos)
- 🐰 **Message Queue (RabbitMQ)**: Event-driven communication

**Removed from Original Plan**:
- ❌ ReviewService (not in C2 diagram scope)
- ❌ Email/SMS notifications (in-app only)
- ❌ Coach/Instructor role (not needed)
