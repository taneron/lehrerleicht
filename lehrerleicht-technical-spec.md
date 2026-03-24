# Lehrerleicht - Technical Specification

> **Purpose:** This document provides complete technical specifications for building the Lehrerleicht application. It is intended for a coding agent to implement the system.

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [System Architecture](#2-system-architecture)
3. [Technology Stack](#3-technology-stack)
4. [C# Microservice Specification (Approval Service)](#4-c-microservice-specification-approval-service)
5. [Node.js Services Specification](#5-nodejs-services-specification)
6. [Database Schema](#6-database-schema)
7. [API Endpoint Documentation](#7-api-endpoint-documentation)
8. [Message Queue Contracts](#8-message-queue-contracts)
9. [Authentication & Authorization](#9-authentication--authorization)
10. [Docker & Deployment](#10-docker--deployment)
11. [Development Setup](#11-development-setup)
12. [Team Endpoint Assignment](#12-team-endpoint-assignment)

---

## 1. Project Overview

### 1.1 What is Lehrerleicht?

Lehrerleicht is a SaaS platform designed to automate administrative tasks for teachers in Austria. It connects to existing school tools (SchoolFox, WebUntis, Email) and uses AI to classify incoming messages, propose actions, and execute them with teacher approval.

### 1.2 Core Value Proposition

```
Message arrives (e.g., "Max is sick today")
    ↓
AI classifies intent → "Absence notification"
    ↓
System proposes action → "Enter absence in WebUntis + confirm to parent"
    ↓
Teacher approves via app (Human-in-the-Loop)
    ↓
System executes automatically
```

### 1.3 Key Components

| Component | Technology | Purpose |
|-----------|------------|---------|
| **Approval Service** | C# ASP.NET 10 | Human-in-the-loop approval workflow (Uni project focus) |
| **Ingestion Service** | TypeScript/Node.js | Poll external APIs, process incoming data |
| **AI Service** | TypeScript + Mastra.ai | Classify messages, determine actions |
| **Execution Service** | TypeScript/Node.js | Execute approved actions on external systems |
| **Web Frontend** | SvelteKit | Teacher dashboard and approval interface |
| **Mobile Apps** | React Native (future) | Mobile approval interface |

---

## 2. System Architecture

### 2.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           EXTERNAL SYSTEMS                                   │
├─────────────────┬─────────────────┬─────────────────┬───────────────────────┤
│  🦊 SchoolFox   │  📅 WebUntis    │  📧 Email       │  📋 Sokrates          │
└────────┬────────┴────────┬────────┴────────┬────────┴───────────┬───────────┘
         │                 │                 │                     │
         ▼                 ▼                 ▼                     ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                    INGESTION SERVICE (TypeScript/Node.js)                    │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │ SF Worker    │  │ WU Worker    │  │ Email Worker │  │ Sok Worker   │     │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘     │
└─────────┼─────────────────┼─────────────────┼─────────────────┼─────────────┘
          │                 │                 │                 │
          ▼                 ▼                 ▼                 ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                              REDIS (BullMQ)                                  │
│         ┌─────────────────┐              ┌─────────────────┐                 │
│         │ ingestion:queue │              │ ai:queue        │                 │
│         └────────┬────────┘              └────────┬────────┘                 │
└──────────────────┼───────────────────────────────┼──────────────────────────┘
                   │                               │
                   ▼                               ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                      AI SERVICE (TypeScript + Mastra.ai)                     │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐           │
│  │ Message          │  │ Action           │  │ Risk             │           │
│  │ Classifier       │  │ Proposer         │  │ Assessor         │           │
│  └────────┬─────────┘  └────────┬─────────┘  └────────┬─────────┘           │
└───────────┼─────────────────────┼─────────────────────┼─────────────────────┘
            │                     │                     │
            ▼                     ▼                     ▼
     ┌──────────────────────────────────────────────────────┐
     │              DECISION: Needs Approval?               │
     └──────────────┬───────────────────────┬───────────────┘
                    │ NO                    │ YES
                    ▼                       ▼
┌───────────────────────────┐    ┌─────────────────────────────────────────────┐
│ EXECUTION SERVICE         │    │          APPROVAL SERVICE (C# ASP.NET)      │
│ (TypeScript)              │    │  ┌─────────────────────────────────────┐    │
│ Auto-execute low-risk     │    │  │ RabbitMQ Consumer (IHostedService)  │    │
│ actions                   │    │  └─────────────────┬───────────────────┘    │
└───────────────────────────┘    │                    ▼                        │
                                 │  ┌─────────────────────────────────────┐    │
                                 │  │ Approval Handler (MediatR)          │    │
                                 │  └─────────────────┬───────────────────┘    │
                                 │                    ▼                        │
                                 │  ┌─────────────────────────────────────┐    │
                                 │  │ PostgreSQL (EF Core)                │    │
                                 │  └─────────────────┬───────────────────┘    │
                                 │                    ▼                        │
                                 │  ┌─────────────────────────────────────┐    │
                                 │  │ SignalR Hub + Push Notifications    │    │
                                 │  └─────────────────┬───────────────────┘    │
                                 └────────────────────┼────────────────────────┘
                                                      │
                    ┌─────────────────────────────────┼─────────────────────────┐
                    │                                 │                         │
                    ▼                                 ▼                         ▼
          ┌─────────────────┐              ┌─────────────────┐       ┌─────────────────┐
          │ 💻 SvelteKit    │              │ 📱 Mobile App   │       │ ⌚ Watch App    │
          │ Web Frontend    │              │ (Future)        │       │ (Future)        │
          └────────┬────────┘              └────────┬────────┘       └────────┬────────┘
                   │                                │                         │
                   └────────────────────────────────┼─────────────────────────┘
                                                    │
                                          Teacher Approves/Rejects
                                                    │
                                                    ▼
                                 ┌─────────────────────────────────────────────┐
                                 │         APPROVAL SERVICE (C# ASP.NET)       │
                                 │  REST API receives decision                 │
                                 │  Publishes to RabbitMQ: approved.actions    │
                                 └─────────────────────┬───────────────────────┘
                                                       │
                                                       ▼
                                 ┌─────────────────────────────────────────────┐
                                 │       EXECUTION SERVICE (TypeScript)        │
                                 │  Consumes approved.actions                  │
                                 │  Writes to SchoolFox / WebUntis / Email     │
                                 └─────────────────────────────────────────────┘
```

### 2.2 Data Flow Summary

```
1. INGEST:    External APIs → Ingestion Workers → Redis Queue → PostgreSQL
2. CLASSIFY:  Redis Queue → AI Service → Risk Assessment
3. ROUTE:     Low Risk → Auto-Execute | High Risk → Approval Service
4. APPROVE:   Approval Service → SignalR/Push → Frontend → User Decision
5. EXECUTE:   Approved Actions → Execution Workers → External APIs
```

---

## 3. Technology Stack

### 3.1 Complete Stack Overview

| Layer | Technology | Version | Purpose |
|-------|------------|---------|---------|
| **C# Microservice** | ASP.NET Core | 10.0 | Approval Service (Uni requirement) |
| **C# ORM** | Entity Framework Core | 10.0 | Database access |
| **C# Auth** | ASP.NET Identity | 10.0 | Authentication/Authorization |
| **C# Real-time** | SignalR | 10.0 | WebSocket communication |
| **Node.js Runtime** | Node.js | 22 LTS | TypeScript services |
| **TypeScript Services** | TypeScript | 5.x | Type-safe Node.js code |
| **AI Framework** | Mastra.ai | Latest | AI agent orchestration |
| **AI SDK** | Vercel AI SDK | Latest | LLM integration (Claude) |
| **Frontend** | SvelteKit | 2.x | Web application |
| **Database** | PostgreSQL | 16 | Primary data store |
| **Cache/Queue** | Redis | 7.x | BullMQ queues + caching |
| **Message Broker** | RabbitMQ | 3.x | Service-to-service messaging |
| **Containerization** | Docker | Latest | Development & deployment |

### 3.2 Service-to-Technology Mapping

```
┌─────────────────────────────────────────────────────────────────┐
│                        SERVICES                                  │
├─────────────────────┬───────────────────────────────────────────┤
│ Approval Service    │ C# ASP.NET 10, EF Core, Identity, SignalR │
├─────────────────────┼───────────────────────────────────────────┤
│ Ingestion Service   │ TypeScript, BullMQ, node-cron             │
├─────────────────────┼───────────────────────────────────────────┤
│ AI Service          │ TypeScript, Mastra.ai, AI SDK, Claude     │
├─────────────────────┼───────────────────────────────────────────┤
│ Execution Service   │ TypeScript, BullMQ                        │
├─────────────────────┼───────────────────────────────────────────┤
│ Web Frontend        │ SvelteKit, TailwindCSS                    │
└─────────────────────┴───────────────────────────────────────────┘
```

---

## 4. C# Microservice Specification (Approval Service)

> **IMPORTANT:** This service must meet university requirements:
> - ASP.NET 10 API Controllers
> - EF Core with PostgreSQL
> - ASP.NET Identity for Auth
> - SignalR for real-time
> - Minimum 4 substantial entities

### 4.1 Project Structure

```
Lehrerleicht.ApprovalService/
│
├── src/
│   ├── Lehrerleicht.Approval.Api/              # ASP.NET Web API
│   │   ├── Controllers/
│   │   │   ├── ApprovalsController.cs
│   │   │   ├── ActionsController.cs
│   │   │   ├── TeachersController.cs
│   │   │   ├── NotificationsController.cs
│   │   │   └── AuthController.cs
│   │   │
│   │   ├── Hubs/
│   │   │   └── ApprovalHub.cs                  # SignalR Hub
│   │   │
│   │   ├── Middleware/
│   │   │   └── ExceptionMiddleware.cs
│   │   │
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── appsettings.Development.json
│   │
│   ├── Lehrerleicht.Approval.Core/             # Domain & Application Logic
│   │   ├── Entities/
│   │   │   ├── Approval.cs
│   │   │   ├── PendingAction.cs
│   │   │   ├── ActionHistory.cs
│   │   │   ├── ActionOption.cs
│   │   │   ├── Teacher.cs
│   │   │   ├── School.cs
│   │   │   └── NotificationPreference.cs
│   │   │
│   │   ├── Enums/
│   │   │   ├── ApprovalStatus.cs
│   │   │   ├── ActionType.cs
│   │   │   ├── ActionSource.cs
│   │   │   ├── ActionOptionType.cs
│   │   │   └── Priority.cs
│   │   │
│   │   ├── DTOs/
│   │   │   ├── ApprovalDto.cs
│   │   │   ├── CreateApprovalDto.cs
│   │   │   ├── ApprovalDecisionDto.cs
│   │   │   ├── ActionOptionDto.cs
│   │   │   ├── TeacherDto.cs
│   │   │   └── ActionHistoryDto.cs
│   │   │
│   │   ├── Interfaces/
│   │   │   ├── IApprovalRepository.cs
│   │   │   ├── IMessagePublisher.cs
│   │   │   └── IPushNotificationService.cs
│   │   │
│   │   └── Services/
│   │       ├── ApprovalService.cs
│   │       ├── NotificationService.cs
│   │       └── ActionHistoryService.cs
│   │
│   └── Lehrerleicht.Approval.Infrastructure/   # Data Access & External
│       ├── Data/
│       │   ├── ApprovalDbContext.cs
│       │   ├── Configurations/
│       │   │   ├── ApprovalConfiguration.cs
│       │   │   ├── PendingActionConfiguration.cs
│       │   │   ├── ActionHistoryConfiguration.cs
│       │   │   ├── ActionOptionConfiguration.cs
│       │   │   ├── TeacherConfiguration.cs
│       │   │   ├── SchoolConfiguration.cs
│       │   │   └── NotificationPreferenceConfiguration.cs
│       │   │
│       │   └── Migrations/
│       │
│       ├── Repositories/
│       │   ├── ApprovalRepository.cs
│       │   ├── TeacherRepository.cs
│       │   └── ActionHistoryRepository.cs
│       │
│       ├── Messaging/
│       │   ├── RabbitMqConsumer.cs
│       │   ├── RabbitMqPublisher.cs
│       │   └── Messages/
│       │       ├── PendingApprovalMessage.cs
│       │       └── ApprovalResultMessage.cs
│       │
│       ├── BackgroundServices/
│       │   ├── ApprovalConsumerService.cs      # IHostedService
│       │   └── ExpiryCheckerService.cs         # IHostedService
│       │
│       └── External/
│           └── FirebasePushService.cs
│
├── tests/
│   ├── Lehrerleicht.Approval.UnitTests/
│   └── Lehrerleicht.Approval.IntegrationTests/
│
├── Dockerfile
└── Lehrerleicht.ApprovalService.sln
```

### 4.2 Entity Definitions (7 Entities)

#### Entity 1: Teacher

```csharp
// Lehrerleicht.Approval.Core/Entities/Teacher.cs

using Microsoft.AspNetCore.Identity;

namespace Lehrerleicht.Approval.Core.Entities;

/// <summary>
/// Represents a teacher user in the system.
/// Extends IdentityUser for authentication.
/// </summary>
public class Teacher : IdentityUser
{
    // Profile Information
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Title { get; set; }  // e.g., "Mag.", "Dr."
    public string? ProfileImageUrl { get; set; }
    
    // School Association
    public Guid SchoolId { get; set; }
    public School School { get; set; } = null!;
    
    // Teaching Info
    public string? Subjects { get; set; }  // Comma-separated: "Mathematik,Physik"
    public string? Classes { get; set; }   // Comma-separated: "3A,4B,5C"
    
    // External System IDs
    public string? SchoolFoxTeacherId { get; set; }
    public string? WebUntisTeacherId { get; set; }
    
    // Settings
    public bool IsActive { get; set; } = true;
    public string PreferredLanguage { get; set; } = "de";
    public string Timezone { get; set; } = "Europe/Vienna";
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation Properties
    public ICollection<Approval> Approvals { get; set; } = new List<Approval>();
    public ICollection<ActionHistory> ActionHistories { get; set; } = new List<ActionHistory>();
    public NotificationPreference? NotificationPreference { get; set; }
}
```

#### Entity 2: School

```csharp
// Lehrerleicht.Approval.Core/Entities/School.cs

namespace Lehrerleicht.Approval.Core.Entities;

/// <summary>
/// Represents a school institution.
/// </summary>
public class School
{
    public Guid Id { get; set; }
    
    // Basic Info
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;  // e.g., "BRG Wien 1"
    public string SchoolCode { get; set; } = string.Empty; // Official Austrian school code
    
    // School Type
    public SchoolType Type { get; set; }  // Volksschule, Mittelschule, AHS, BHS, etc.
    
    // Address
    public string Street { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = "Austria";
    public string? State { get; set; }  // Bundesland
    
    // Contact
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    
    // External System Configuration
    public string? SchoolFoxSchoolId { get; set; }
    public string? WebUntisSchoolName { get; set; }
    public string? WebUntisServer { get; set; }
    
    // Subscription
    public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Free;
    public DateTime? SubscriptionExpiresAt { get; set; }
    
    // Settings
    public bool IsActive { get; set; } = true;
    public int DefaultApprovalExpiryHours { get; set; } = 24;
    public string Timezone { get; set; } = "Europe/Vienna";
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation Properties
    public ICollection<Teacher> Teachers { get; set; } = new List<Teacher>();
}

public enum SchoolType
{
    Volksschule,
    Mittelschule,
    AHS,
    BHS,
    Berufsschule,
    Other
}

public enum SubscriptionTier
{
    Free,
    Basic,
    Professional,
    Enterprise
}
```

#### Entity 3: Approval (with PendingAction as Value Object)

```csharp
// Lehrerleicht.Approval.Core/Entities/Approval.cs

namespace Lehrerleicht.Approval.Core.Entities;

/// <summary>
/// Represents an approval request for an AI-proposed action.
/// This is the core entity of the approval workflow.
/// </summary>
public class Approval
{
    public Guid Id { get; set; }
    
    // Correlation
    public Guid CorrelationId { get; set; }  // Links to original message in other services
    
    // Teacher Assignment
    public string TeacherId { get; set; } = string.Empty;
    public Teacher Teacher { get; set; } = null!;
    
    // Status
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    
    // The Pending Action (embedded entity)
    public PendingAction Action { get; set; } = null!;
    
    // Timing
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    
    // Decision Details (filled when processed)
    public string? ProcessedByDeviceId { get; set; }
    public string? ProcessedByDeviceType { get; set; }  // "web", "mobile", "watch"
    public string? RejectionReason { get; set; }
    
    // Notification Tracking
    public bool PushNotificationSent { get; set; } = false;
    public DateTime? PushNotificationSentAt { get; set; }
    public bool EmailNotificationSent { get; set; } = false;
    public DateTime? EmailNotificationSentAt { get; set; }
    
    // Read Status
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    
    // Navigation Properties
    public ICollection<ActionHistory> History { get; set; } = new List<ActionHistory>();
}

/// <summary>
/// The proposed action that needs approval.
/// Stored as a related entity to Approval.
/// </summary>
public class PendingAction
{
    public Guid Id { get; set; }
    
    // Parent
    public Guid ApprovalId { get; set; }
    public Approval Approval { get; set; } = null!;
    
    // Action Classification
    public ActionType Type { get; set; }
    public ActionSource Source { get; set; }
    public Priority Priority { get; set; } = Priority.Normal;
    
    // Display Info
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    
    // Context (who/what is this about)
    public string? StudentId { get; set; }
    public string? StudentName { get; set; }
    public string? ClassName { get; set; }
    public string? ParentName { get; set; }
    
    // The Actual Action Data
    public string PayloadJson { get; set; } = "{}";  // JSON of action details
    public string TargetSystem { get; set; } = string.Empty;  // "WebUntis", "SchoolFox", etc.
    
    // Original Message Reference
    public string? OriginalMessageId { get; set; }
    public string? OriginalMessagePreview { get; set; }
    public DateTime? OriginalMessageTimestamp { get; set; }
    
    // AI Analysis
    public double ConfidenceScore { get; set; }  // 0.0 - 1.0
    public string? AiReasoning { get; set; }  // Why AI proposed this action

    // Navigation Properties
    public ICollection<ActionOption> Options { get; set; } = new List<ActionOption>();
}

// Lehrerleicht.Approval.Core/Enums/ApprovalStatus.cs
public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected,
    Expired,
    Cancelled
}

// Lehrerleicht.Approval.Core/Enums/ActionType.cs
public enum ActionType
{
    AbsenceEntry,           // Enter student absence
    AbsenceConfirmation,    // Confirm absence to parent
    MessageReply,           // Reply to parent message
    GradeEntry,             // Enter a grade
    HomeworkAssignment,     // Assign homework
    EventReminder,          // Send reminder about event
    DocumentShare,          // Share document with parents
    MeetingSchedule,        // Schedule parent-teacher meeting
    BehaviorNote,           // Document student behavior
    Other
}

// Lehrerleicht.Approval.Core/Enums/ActionSource.cs
public enum ActionSource
{
    SchoolFox,
    WebUntis,
    Email,
    Sokrates,
    Teams,
    Manual,
    VoiceMemo
}

// Lehrerleicht.Approval.Core/Enums/Priority.cs
public enum Priority
{
    Low,
    Normal,
    High,
    Urgent
}
```

#### Entity 4: ActionOption

```csharp
// Lehrerleicht.Approval.Core/Entities/ActionOption.cs

namespace Lehrerleicht.Approval.Core.Entities;

/// <summary>
/// Represents an option or question the teacher must respond to when approving an action.
/// Used when a simple yes/no is not enough — e.g., multiselect choices, open-ended text input,
/// or single-select options that the AI needs the teacher to decide on.
/// </summary>
public class ActionOption
{
    public Guid Id { get; set; }

    // Parent
    public Guid PendingActionId { get; set; }
    public PendingAction PendingAction { get; set; } = null!;

    // Option Definition
    public ActionOptionType Type { get; set; }  // SingleSelect, MultiSelect, FreeText, Date, Confirm
    public string Label { get; set; } = string.Empty;  // Question or prompt shown to teacher
    public string? HelpText { get; set; }  // Additional explanation
    public bool IsRequired { get; set; } = true;
    public int SortOrder { get; set; } = 0;  // Display order

    // Available Choices (JSON array for SingleSelect/MultiSelect, null for FreeText)
    // e.g. ["Entschuldigt", "Unentschuldigt", "Arztbesuch"]
    public string? ChoicesJson { get; set; }

    // Teacher's Response (filled when teacher approves)
    public string? SelectedValueJson { get; set; }  // JSON: string for FreeText, array for MultiSelect

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Lehrerleicht.Approval.Core/Enums/ActionOptionType.cs
public enum ActionOptionType
{
    SingleSelect,    // Teacher picks one from a list
    MultiSelect,     // Teacher picks one or more from a list
    FreeText,        // Teacher types a response
    Date,            // Teacher picks a date
    Confirm          // Teacher must explicitly confirm something (checkbox)
}
```

#### Entity 5: ActionHistory

```csharp
// Lehrerleicht.Approval.Core/Entities/ActionHistory.cs

namespace Lehrerleicht.Approval.Core.Entities;

/// <summary>
/// Audit trail for all actions on approvals.
/// Records every state change and user interaction.
/// </summary>
public class ActionHistory
{
    public Guid Id { get; set; }
    
    // References
    public Guid ApprovalId { get; set; }
    public Approval Approval { get; set; } = null!;
    
    public string? TeacherId { get; set; }
    public Teacher? Teacher { get; set; }
    
    // Action Details
    public ActionHistoryType ActionType { get; set; }
    public string Description { get; set; } = string.Empty;
    
    // Previous and New State (for state changes)
    public string? PreviousState { get; set; }
    public string? NewState { get; set; }
    
    // Additional Data
    public string? AdditionalDataJson { get; set; }  // JSON for extra info
    
    // Device/Context Info
    public string? DeviceId { get; set; }
    public string? DeviceType { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    
    // Timestamp
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public enum ActionHistoryType
{
    Created,
    Viewed,
    Approved,
    Rejected,
    Expired,
    Cancelled,
    NotificationSent,
    ExecutionStarted,
    ExecutionCompleted,
    ExecutionFailed,
    Modified
}
```

#### Entity 6: NotificationPreference

```csharp
// Lehrerleicht.Approval.Core/Entities/NotificationPreference.cs

namespace Lehrerleicht.Approval.Core.Entities;

/// <summary>
/// Teacher's notification preferences and device registrations.
/// </summary>
public class NotificationPreference
{
    public Guid Id { get; set; }
    
    // Owner
    public string TeacherId { get; set; } = string.Empty;
    public Teacher Teacher { get; set; } = null!;
    
    // Push Notification Settings
    public bool PushEnabled { get; set; } = true;
    public bool PushForHighPriority { get; set; } = true;
    public bool PushForNormalPriority { get; set; } = true;
    public bool PushForLowPriority { get; set; } = false;
    
    // Email Notification Settings
    public bool EmailEnabled { get; set; } = true;
    public bool EmailDigestEnabled { get; set; } = false;
    public DigestFrequency EmailDigestFrequency { get; set; } = DigestFrequency.Daily;
    
    // Quiet Hours
    public bool QuietHoursEnabled { get; set; } = true;
    public TimeOnly QuietHoursStart { get; set; } = new TimeOnly(20, 0);  // 8 PM
    public TimeOnly QuietHoursEnd { get; set; } = new TimeOnly(7, 0);    // 7 AM
    public bool QuietHoursWeekendAllDay { get; set; } = true;
    
    // Device Tokens (for push notifications)
    public string? FcmToken { get; set; }        // Firebase Cloud Messaging
    public string? ApnsToken { get; set; }       // Apple Push Notification
    public string? WebPushSubscription { get; set; }  // Web Push JSON
    
    // Last Updated
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum DigestFrequency
{
    Daily,
    Weekly,
    Never
}
```

### 4.3 DbContext Configuration

```csharp
// Lehrerleicht.Approval.Infrastructure/Data/ApprovalDbContext.cs

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Lehrerleicht.Approval.Core.Entities;

namespace Lehrerleicht.Approval.Infrastructure.Data;

public class ApprovalDbContext : IdentityDbContext<Teacher>
{
    public ApprovalDbContext(DbContextOptions<ApprovalDbContext> options) 
        : base(options)
    {
    }
    
    public DbSet<School> Schools => Set<School>();
    public DbSet<Approval> Approvals => Set<Approval>();
    public DbSet<PendingAction> PendingActions => Set<PendingAction>();
    public DbSet<ActionOption> ActionOptions => Set<ActionOption>();
    public DbSet<ActionHistory> ActionHistories => Set<ActionHistory>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.ApplyConfigurationsFromAssembly(typeof(ApprovalDbContext).Assembly);
        
        // Rename Identity tables to match our naming convention
        builder.Entity<Teacher>().ToTable("teachers");
        builder.Entity<School>().ToTable("schools");
        builder.Entity<Approval>().ToTable("approvals");
        builder.Entity<PendingAction>().ToTable("pending_actions");
        builder.Entity<ActionOption>().ToTable("action_options");
        builder.Entity<ActionHistory>().ToTable("action_histories");
        builder.Entity<NotificationPreference>().ToTable("notification_preferences");
    }
}
```

### 4.4 API Controllers

```csharp
// Lehrerleicht.Approval.Api/Controllers/ApprovalsController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lehrerleicht.Approval.Core.DTOs;
using Lehrerleicht.Approval.Core.Interfaces;

namespace Lehrerleicht.Approval.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApprovalsController : ControllerBase
{
    private readonly IApprovalService _approvalService;
    private readonly ILogger<ApprovalsController> _logger;
    
    public ApprovalsController(
        IApprovalService approvalService,
        ILogger<ApprovalsController> logger)
    {
        _approvalService = approvalService;
        _logger = logger;
    }
    
    /// <summary>
    /// Get all pending approvals for the current teacher
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApprovalDto>>> GetPendingApprovals(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null)
    {
        var teacherId = User.FindFirst("sub")?.Value;
        var approvals = await _approvalService.GetApprovalsAsync(
            teacherId!, page, pageSize, status, priority);
        return Ok(approvals);
    }
    
    /// <summary>
    /// Get a specific approval by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApprovalDetailDto>> GetApproval(Guid id)
    {
        var approval = await _approvalService.GetApprovalByIdAsync(id);
        if (approval == null) return NotFound();
        return Ok(approval);
    }
    
    /// <summary>
    /// Approve an action
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApprovalResultDto>> Approve(
        Guid id,
        [FromBody] ApprovalDecisionDto decision)
    {
        var teacherId = User.FindFirst("sub")?.Value;
        var result = await _approvalService.ApproveAsync(id, teacherId!, decision);
        return Ok(result);
    }
    
    /// <summary>
    /// Reject an action
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ApprovalResultDto>> Reject(
        Guid id,
        [FromBody] RejectionDto rejection)
    {
        var teacherId = User.FindFirst("sub")?.Value;
        var result = await _approvalService.RejectAsync(id, teacherId!, rejection);
        return Ok(result);
    }
    
    /// <summary>
    /// Mark an approval as read
    /// </summary>
    [HttpPost("{id:guid}/read")]
    public async Task<ActionResult> MarkAsRead(Guid id)
    {
        await _approvalService.MarkAsReadAsync(id);
        return NoContent();
    }
    
    /// <summary>
    /// Get approval statistics for the current teacher
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<ApprovalStatsDto>> GetStats()
    {
        var teacherId = User.FindFirst("sub")?.Value;
        var stats = await _approvalService.GetStatsAsync(teacherId!);
        return Ok(stats);
    }
}
```

### 4.5 SignalR Hub

```csharp
// Lehrerleicht.Approval.Api/Hubs/ApprovalHub.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Lehrerleicht.Approval.Core.DTOs;

namespace Lehrerleicht.Approval.Api.Hubs;

[Authorize]
public class ApprovalHub : Hub
{
    private readonly ILogger<ApprovalHub> _logger;
    
    public ApprovalHub(ILogger<ApprovalHub> logger)
    {
        _logger = logger;
    }
    
    public override async Task OnConnectedAsync()
    {
        var teacherId = Context.User?.FindFirst("sub")?.Value;
        if (teacherId != null)
        {
            // Add to teacher-specific group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"teacher:{teacherId}");
            _logger.LogInformation("Teacher {TeacherId} connected to ApprovalHub", teacherId);
        }
        await base.OnConnectedAsync();
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var teacherId = Context.User?.FindFirst("sub")?.Value;
        if (teacherId != null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"teacher:{teacherId}");
            _logger.LogInformation("Teacher {TeacherId} disconnected from ApprovalHub", teacherId);
        }
        await base.OnDisconnectedAsync(exception);
    }
    
    // Called by server to notify clients
    public async Task SendNewApproval(string teacherId, ApprovalDto approval)
    {
        await Clients.Group($"teacher:{teacherId}").SendAsync("NewApproval", approval);
    }
    
    public async Task SendApprovalProcessed(string teacherId, Guid approvalId, string status)
    {
        await Clients.Group($"teacher:{teacherId}").SendAsync("ApprovalProcessed", approvalId, status);
    }
    
    public async Task SendApprovalExpired(string teacherId, Guid approvalId)
    {
        await Clients.Group($"teacher:{teacherId}").SendAsync("ApprovalExpired", approvalId);
    }
    
    // Can be called by clients
    public async Task SubscribeToSchool(string schoolId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"school:{schoolId}");
    }
}
```

---

## 5. Node.js Services Specification

### 5.1 Shared Package Structure

```
packages/
├── shared/                          # Shared types and utilities
│   ├── src/
│   │   ├── types/
│   │   │   ├── approval.ts          # Approval-related types
│   │   │   ├── action.ts            # Action types
│   │   │   ├── message.ts           # Message queue types
│   │   │   └── index.ts
│   │   ├── utils/
│   │   │   ├── logger.ts
│   │   │   ├── encryption.ts
│   │   │   └── validation.ts
│   │   └── index.ts
│   ├── package.json
│   └── tsconfig.json
│
├── ingestion-service/               # Data ingestion from external APIs
│   ├── src/
│   │   ├── workers/
│   │   │   ├── schoolfox.worker.ts
│   │   │   ├── webuntis.worker.ts
│   │   │   └── email.worker.ts
│   │   ├── clients/
│   │   │   ├── schoolfox.client.ts
│   │   │   ├── webuntis.client.ts
│   │   │   └── imap.client.ts
│   │   ├── queue/
│   │   │   └── ingestion.queue.ts
│   │   └── index.ts
│   ├── package.json
│   ├── Dockerfile
│   └── tsconfig.json
│
├── ai-service/                      # AI classification and action proposal
│   ├── src/
│   │   ├── agents/
│   │   │   ├── classifier.agent.ts
│   │   │   ├── action-proposer.agent.ts
│   │   │   └── risk-assessor.agent.ts
│   │   ├── tools/
│   │   │   ├── lookup-student.tool.ts
│   │   │   ├── check-calendar.tool.ts
│   │   │   └── get-context.tool.ts
│   │   ├── workflows/
│   │   │   └── message-processing.workflow.ts
│   │   ├── queue/
│   │   │   └── ai.queue.ts
│   │   └── index.ts
│   ├── package.json
│   ├── Dockerfile
│   └── tsconfig.json
│
├── execution-service/               # Execute approved actions
│   ├── src/
│   │   ├── executors/
│   │   │   ├── schoolfox.executor.ts
│   │   │   ├── webuntis.executor.ts
│   │   │   └── email.executor.ts
│   │   ├── queue/
│   │   │   └── execution.queue.ts
│   │   └── index.ts
│   ├── package.json
│   ├── Dockerfile
│   └── tsconfig.json
│
└── web-frontend/                    # SvelteKit frontend
    ├── src/
    │   ├── routes/
    │   │   ├── +page.svelte
    │   │   ├── +layout.svelte
    │   │   ├── login/
    │   │   ├── dashboard/
    │   │   ├── approvals/
    │   │   │   ├── +page.svelte
    │   │   │   └── [id]/
    │   │   ├── history/
    │   │   └── settings/
    │   ├── lib/
    │   │   ├── components/
    │   │   ├── stores/
    │   │   ├── api/
    │   │   └── signalr/
    │   └── app.html
    ├── package.json
    ├── Dockerfile
    ├── svelte.config.js
    └── tsconfig.json
```

### 5.2 AI Service with Mastra.ai

```typescript
// packages/ai-service/src/agents/classifier.agent.ts

import { Agent } from '@mastra/core';
import { createAnthropic } from '@ai-sdk/anthropic';

const anthropic = createAnthropic({
  apiKey: process.env.ANTHROPIC_API_KEY!,
});

export const classifierAgent = new Agent({
  name: 'MessageClassifier',
  description: 'Classifies incoming school messages and determines intent',
  model: anthropic('claude-sonnet-4-20250514'),
  
  instructions: `
    You are an expert at classifying messages in an Austrian school context.
    
    Analyze the incoming message and determine:
    1. Message Type (absence_notification, question, complaint, information, request, etc.)
    2. Urgency (low, normal, high, urgent)
    3. Required Actions (what needs to be done)
    4. Entities (student names, dates, classes mentioned)
    
    Always respond in JSON format.
  `,
  
  tools: {
    lookupStudent: {
      description: 'Look up student information by name',
      parameters: {
        name: { type: 'string', description: 'Student name to look up' },
      },
      execute: async ({ name }) => {
        // Implementation
      },
    },
  },
});

// packages/ai-service/src/workflows/message-processing.workflow.ts

import { Workflow, Step } from '@mastra/core';
import { classifierAgent } from '../agents/classifier.agent';
import { actionProposerAgent } from '../agents/action-proposer.agent';
import { riskAssessorAgent } from '../agents/risk-assessor.agent';

export const messageProcessingWorkflow = new Workflow({
  name: 'ProcessIncomingMessage',
  
  steps: [
    new Step({
      id: 'classify',
      agent: classifierAgent,
      input: (ctx) => ({
        message: ctx.input.message,
        source: ctx.input.source,
        metadata: ctx.input.metadata,
      }),
    }),
    
    new Step({
      id: 'proposeAction',
      agent: actionProposerAgent,
      input: (ctx) => ({
        classification: ctx.steps.classify.output,
        message: ctx.input.message,
      }),
    }),
    
    new Step({
      id: 'assessRisk',
      agent: riskAssessorAgent,
      input: (ctx) => ({
        action: ctx.steps.proposeAction.output,
        classification: ctx.steps.classify.output,
      }),
    }),
    
    new Step({
      id: 'route',
      execute: async (ctx) => {
        const risk = ctx.steps.assessRisk.output;
        const action = ctx.steps.proposeAction.output;
        
        if (risk.needsApproval) {
          // Publish to RabbitMQ: pending.approvals
          await publishToApprovalQueue({
            correlationId: ctx.input.correlationId,
            teacherId: ctx.input.teacherId,
            action: action,
            risk: risk,
          });
          return { routed: 'approval' };
        } else {
          // Publish to BullMQ: execution queue
          await publishToExecutionQueue({
            correlationId: ctx.input.correlationId,
            action: action,
          });
          return { routed: 'auto-execute' };
        }
      },
    }),
  ],
});
```

### 5.3 SvelteKit Frontend Structure

```typescript
// packages/web-frontend/src/lib/api/approvals.ts

import type { Approval, ApprovalDecision } from '@lehrerleicht/shared';

const API_BASE = import.meta.env.VITE_API_URL;

export async function getPendingApprovals(token: string): Promise<Approval[]> {
  const response = await fetch(`${API_BASE}/api/approvals`, {
    headers: {
      'Authorization': `Bearer ${token}`,
    },
  });
  return response.json();
}

export async function approveAction(
  id: string, 
  decision: ApprovalDecision,
  token: string
): Promise<void> {
  await fetch(`${API_BASE}/api/approvals/${id}/approve`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(decision),
  });
}

// packages/web-frontend/src/lib/signalr/connection.ts

import * as signalR from '@microsoft/signalr';
import { approvalStore } from '../stores/approvals';

let connection: signalR.HubConnection | null = null;

export async function connectToHub(token: string) {
  connection = new signalR.HubConnectionBuilder()
    .withUrl(`${import.meta.env.VITE_API_URL}/hubs/approvals`, {
      accessTokenFactory: () => token,
    })
    .withAutomaticReconnect()
    .build();
  
  connection.on('NewApproval', (approval) => {
    approvalStore.addApproval(approval);
  });
  
  connection.on('ApprovalProcessed', (id, status) => {
    approvalStore.updateStatus(id, status);
  });
  
  connection.on('ApprovalExpired', (id) => {
    approvalStore.markExpired(id);
  });
  
  await connection.start();
}
```

---

## 6. Database Schema

### 6.1 PostgreSQL Schema (C# Service)

```sql
-- Generated by EF Core Migrations
-- Database: lehrerleicht_approvals

-- Schools table
CREATE TABLE schools (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(200) NOT NULL,
    short_name VARCHAR(50) NOT NULL,
    school_code VARCHAR(20) NOT NULL UNIQUE,
    type INTEGER NOT NULL,
    street VARCHAR(200) NOT NULL,
    postal_code VARCHAR(10) NOT NULL,
    city VARCHAR(100) NOT NULL,
    country VARCHAR(50) NOT NULL DEFAULT 'Austria',
    state VARCHAR(50),
    phone VARCHAR(30),
    email VARCHAR(200),
    website VARCHAR(200),
    school_fox_school_id VARCHAR(100),
    web_untis_school_name VARCHAR(100),
    web_untis_server VARCHAR(200),
    subscription_tier INTEGER NOT NULL DEFAULT 0,
    subscription_expires_at TIMESTAMPTZ,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    default_approval_expiry_hours INTEGER NOT NULL DEFAULT 24,
    timezone VARCHAR(50) NOT NULL DEFAULT 'Europe/Vienna',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Teachers table (extends ASP.NET Identity)
CREATE TABLE teachers (
    id VARCHAR(450) PRIMARY KEY,
    user_name VARCHAR(256),
    normalized_user_name VARCHAR(256),
    email VARCHAR(256),
    normalized_email VARCHAR(256),
    email_confirmed BOOLEAN NOT NULL,
    password_hash TEXT,
    security_stamp TEXT,
    concurrency_stamp TEXT,
    phone_number VARCHAR(30),
    phone_number_confirmed BOOLEAN NOT NULL,
    two_factor_enabled BOOLEAN NOT NULL,
    lockout_end TIMESTAMPTZ,
    lockout_enabled BOOLEAN NOT NULL,
    access_failed_count INTEGER NOT NULL,
    -- Custom fields
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    title VARCHAR(20),
    profile_image_url VARCHAR(500),
    school_id UUID NOT NULL REFERENCES schools(id),
    subjects VARCHAR(500),
    classes VARCHAR(500),
    school_fox_teacher_id VARCHAR(100),
    web_untis_teacher_id VARCHAR(100),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    preferred_language VARCHAR(10) NOT NULL DEFAULT 'de',
    timezone VARCHAR(50) NOT NULL DEFAULT 'Europe/Vienna',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_login_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Approvals table
CREATE TABLE approvals (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    correlation_id UUID NOT NULL,
    teacher_id VARCHAR(450) NOT NULL REFERENCES teachers(id),
    status INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ NOT NULL,
    processed_at TIMESTAMPTZ,
    processed_by_device_id VARCHAR(200),
    processed_by_device_type VARCHAR(20),
    rejection_reason TEXT,
    push_notification_sent BOOLEAN NOT NULL DEFAULT FALSE,
    push_notification_sent_at TIMESTAMPTZ,
    email_notification_sent BOOLEAN NOT NULL DEFAULT FALSE,
    email_notification_sent_at TIMESTAMPTZ,
    is_read BOOLEAN NOT NULL DEFAULT FALSE,
    read_at TIMESTAMPTZ
);

-- Pending Actions table
CREATE TABLE pending_actions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    approval_id UUID NOT NULL UNIQUE REFERENCES approvals(id) ON DELETE CASCADE,
    type INTEGER NOT NULL,
    source INTEGER NOT NULL,
    priority INTEGER NOT NULL DEFAULT 1,
    title VARCHAR(300) NOT NULL,
    description TEXT NOT NULL,
    icon_url VARCHAR(500),
    student_id VARCHAR(100),
    student_name VARCHAR(200),
    class_name VARCHAR(50),
    parent_name VARCHAR(200),
    payload_json JSONB NOT NULL DEFAULT '{}',
    target_system VARCHAR(50) NOT NULL,
    original_message_id VARCHAR(200),
    original_message_preview VARCHAR(500),
    original_message_timestamp TIMESTAMPTZ,
    confidence_score DECIMAL(3,2) NOT NULL DEFAULT 0.0,
    ai_reasoning TEXT
);

-- Action History table
CREATE TABLE action_histories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    approval_id UUID NOT NULL REFERENCES approvals(id) ON DELETE CASCADE,
    teacher_id VARCHAR(450) REFERENCES teachers(id),
    action_type INTEGER NOT NULL,
    description VARCHAR(500) NOT NULL,
    previous_state VARCHAR(50),
    new_state VARCHAR(50),
    additional_data_json JSONB,
    device_id VARCHAR(200),
    device_type VARCHAR(50),
    ip_address VARCHAR(45),
    user_agent VARCHAR(500),
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Notification Preferences table
CREATE TABLE notification_preferences (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    teacher_id VARCHAR(450) NOT NULL UNIQUE REFERENCES teachers(id) ON DELETE CASCADE,
    push_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    push_for_high_priority BOOLEAN NOT NULL DEFAULT TRUE,
    push_for_normal_priority BOOLEAN NOT NULL DEFAULT TRUE,
    push_for_low_priority BOOLEAN NOT NULL DEFAULT FALSE,
    email_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    email_digest_enabled BOOLEAN NOT NULL DEFAULT FALSE,
    email_digest_frequency INTEGER NOT NULL DEFAULT 0,
    quiet_hours_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    quiet_hours_start TIME NOT NULL DEFAULT '20:00',
    quiet_hours_end TIME NOT NULL DEFAULT '07:00',
    quiet_hours_weekend_all_day BOOLEAN NOT NULL DEFAULT TRUE,
    fcm_token TEXT,
    apns_token TEXT,
    web_push_subscription TEXT,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Indexes
CREATE INDEX idx_teachers_school ON teachers(school_id);
CREATE INDEX idx_teachers_email ON teachers(normalized_email);
CREATE INDEX idx_approvals_teacher_status ON approvals(teacher_id, status);
CREATE INDEX idx_approvals_expires ON approvals(expires_at) WHERE status = 0;
CREATE INDEX idx_approvals_correlation ON approvals(correlation_id);
CREATE INDEX idx_pending_actions_type ON pending_actions(type);
CREATE INDEX idx_action_histories_approval ON action_histories(approval_id);
CREATE INDEX idx_action_histories_timestamp ON action_histories(timestamp);
```

---

## 7. API Endpoint Documentation

### 7.1 C# Approval Service Endpoints

| Method | Endpoint | Description | Auth | Assignee |
|--------|----------|-------------|------|----------|
| **Auth** |
| POST | `/api/auth/register` | Register new teacher | No | Member 1 |
| POST | `/api/auth/login` | Login and get JWT | No | Member 1 |
| POST | `/api/auth/refresh` | Refresh JWT token | Yes | Member 1 |
| POST | `/api/auth/logout` | Logout (invalidate token) | Yes | Member 1 |
| GET | `/api/auth/me` | Get current user info | Yes | Member 1 |
| **Teachers** |
| GET | `/api/teachers` | List teachers (admin) | Yes (Admin) | Member 1 |
| GET | `/api/teachers/{id}` | Get teacher by ID | Yes | Member 1 |
| PUT | `/api/teachers/{id}` | Update teacher profile | Yes | Member 1 |
| DELETE | `/api/teachers/{id}` | Deactivate teacher | Yes (Admin) | Member 1 |
| **Approvals** |
| GET | `/api/approvals` | List pending approvals | Yes | Member 2 |
| GET | `/api/approvals/{id}` | Get approval details | Yes | Member 2 |
| POST | `/api/approvals/{id}/approve` | Approve an action | Yes | Member 2 |
| POST | `/api/approvals/{id}/reject` | Reject an action | Yes | Member 2 |
| POST | `/api/approvals/{id}/read` | Mark as read | Yes | Member 2 |
| GET | `/api/approvals/stats` | Get approval statistics | Yes | Member 2 |
| **Actions** |
| GET | `/api/actions/history` | Get action history | Yes | Member 3 |
| GET | `/api/actions/history/{id}` | Get specific history entry | Yes | Member 3 |
| GET | `/api/actions/by-student/{studentId}` | Actions for student | Yes | Member 3 |
| **Notifications** |
| GET | `/api/notifications/preferences` | Get notification prefs | Yes | Member 3 |
| PUT | `/api/notifications/preferences` | Update notification prefs | Yes | Member 3 |
| POST | `/api/notifications/register-device` | Register push token | Yes | Member 3 |
| DELETE | `/api/notifications/unregister-device` | Remove push token | Yes | Member 3 |
| **Schools** |
| GET | `/api/schools` | List schools (admin) | Yes (Admin) | Member 4 |
| GET | `/api/schools/{id}` | Get school details | Yes | Member 4 |
| POST | `/api/schools` | Create school (admin) | Yes (Admin) | Member 4 |
| PUT | `/api/schools/{id}` | Update school | Yes (Admin) | Member 4 |
| **Health** |
| GET | `/health` | Health check | No | Member 4 |
| GET | `/health/ready` | Readiness probe | No | Member 4 |

### 7.2 SignalR Hub Methods

| Method | Direction | Description |
|--------|-----------|-------------|
| `NewApproval` | Server → Client | New approval needs attention |
| `ApprovalProcessed` | Server → Client | Approval was processed |
| `ApprovalExpired` | Server → Client | Approval expired |
| `SubscribeToSchool` | Client → Server | Subscribe to school-wide events |

---

## 8. Message Queue Contracts

### 8.1 RabbitMQ Exchanges and Queues

```
Exchange: lehrerleicht.approvals (topic)
├── Queue: pending.approvals
│   └── Routing Key: approval.pending
├── Queue: approved.actions  
│   └── Routing Key: approval.approved
├── Queue: rejected.actions
│   └── Routing Key: approval.rejected
└── Queue: expired.approvals
    └── Routing Key: approval.expired
```

### 8.2 Message Schemas

#### Pending Approval Message (AI → C#)

```typescript
interface PendingApprovalMessage {
  correlationId: string;      // UUID
  teacherId: string;          // Teacher's user ID
  schoolId: string;           // School ID
  
  action: {
    type: ActionType;
    source: ActionSource;
    priority: Priority;
    title: string;
    description: string;
    
    // Context
    studentId?: string;
    studentName?: string;
    className?: string;
    parentName?: string;
    
    // Action payload
    payload: Record<string, unknown>;
    targetSystem: string;
    
    // Original message
    originalMessageId?: string;
    originalMessagePreview?: string;
    originalMessageTimestamp?: string;
    
    // AI metadata
    confidenceScore: number;
    aiReasoning?: string;

    // Options the teacher must respond to (when simple yes/no is not enough)
    options?: Array<{
      type: 'SingleSelect' | 'MultiSelect' | 'FreeText' | 'Date' | 'Confirm';
      label: string;
      helpText?: string;
      isRequired: boolean;
      sortOrder: number;
      choices?: string[];  // Available choices for SingleSelect/MultiSelect
    }>;
  };

  expiresAt: string;          // ISO timestamp
  createdAt: string;          // ISO timestamp
}
```

#### Approval Result Message (C# → Node.js)

```typescript
interface ApprovalResultMessage {
  correlationId: string;
  approvalId: string;
  teacherId: string;
  
  status: 'approved' | 'rejected' | 'expired';
  processedAt: string;
  
  action: {
    type: ActionType;
    source: ActionSource;
    payload: Record<string, unknown>;
    targetSystem: string;
  };

  // Teacher's selected option values (when ActionOptions were present)
  selectedOptions?: Array<{
    label: string;
    type: string;
    selectedValue: unknown;  // string, string[], or date depending on type
  }>;

  rejectionReason?: string;
}
```

---

## 9. Authentication & Authorization

### 9.1 JWT Configuration

```csharp
// Program.cs configuration

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
    
    // For SignalR
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("TeacherOnly", policy => policy.RequireRole("Teacher", "Admin"));
});
```

### 9.2 Roles

| Role | Permissions |
|------|-------------|
| `Admin` | Full access, manage schools, manage teachers |
| `Teacher` | Own approvals, own profile, own history |

---

## 10. Docker & Deployment

### 10.1 Docker Compose

```yaml
# docker-compose.yml

version: '3.8'

services:
  # ============ DATABASES ============
  postgres:
    image: postgres:16-alpine
    container_name: lehrerleicht-postgres
    environment:
      POSTGRES_USER: lehrerleicht
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:-devpassword}
      POSTGRES_DB: lehrerleicht
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./scripts/init-db.sql:/docker-entrypoint-initdb.d/init.sql
    ports:
      - "5432:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U lehrerleicht"]
      interval: 5s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    container_name: lehrerleicht-redis
    command: redis-server --appendonly yes
    volumes:
      - redis_data:/data
    ports:
      - "6379:6379"
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 5s
      timeout: 5s
      retries: 5

  rabbitmq:
    image: rabbitmq:3-management-alpine
    container_name: lehrerleicht-rabbitmq
    environment:
      RABBITMQ_DEFAULT_USER: lehrerleicht
      RABBITMQ_DEFAULT_PASS: ${RABBITMQ_PASSWORD:-devpassword}
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
    ports:
      - "5672:5672"
      - "15672:15672"  # Management UI
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "check_running"]
      interval: 10s
      timeout: 10s
      retries: 5

  # ============ C# APPROVAL SERVICE ============
  approval-service:
    build:
      context: ./services/approval-service
      dockerfile: Dockerfile
    container_name: lehrerleicht-approval
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://+:5000
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=lehrerleicht;Username=lehrerleicht;Password=${POSTGRES_PASSWORD:-devpassword}"
      ConnectionStrings__Redis: "redis:6379"
      RabbitMq__Host: rabbitmq
      RabbitMq__Username: lehrerleicht
      RabbitMq__Password: ${RABBITMQ_PASSWORD:-devpassword}
      Jwt__Key: ${JWT_SECRET_KEY:-your-super-secret-key-min-32-chars!!}
      Jwt__Issuer: lehrerleicht
      Jwt__Audience: lehrerleicht-clients
    ports:
      - "5000:5000"
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy
      rabbitmq:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
      interval: 10s
      timeout: 5s
      retries: 5

  # ============ NODE.JS SERVICES ============
  ingestion-service:
    build:
      context: ./services/ingestion-service
      dockerfile: Dockerfile
    container_name: lehrerleicht-ingestion
    environment:
      NODE_ENV: development
      REDIS_URL: redis://redis:6379
      POSTGRES_URL: postgres://lehrerleicht:${POSTGRES_PASSWORD:-devpassword}@postgres:5432/lehrerleicht
      RABBITMQ_URL: amqp://lehrerleicht:${RABBITMQ_PASSWORD:-devpassword}@rabbitmq:5672
      # External API keys (mount via secrets in production)
      SCHOOLFOX_API_KEY: ${SCHOOLFOX_API_KEY:-}
      WEBUNTIS_USERNAME: ${WEBUNTIS_USERNAME:-}
      WEBUNTIS_PASSWORD: ${WEBUNTIS_PASSWORD:-}
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy
      rabbitmq:
        condition: service_healthy

  ai-service:
    build:
      context: ./services/ai-service
      dockerfile: Dockerfile
    container_name: lehrerleicht-ai
    environment:
      NODE_ENV: development
      REDIS_URL: redis://redis:6379
      POSTGRES_URL: postgres://lehrerleicht:${POSTGRES_PASSWORD:-devpassword}@postgres:5432/lehrerleicht
      RABBITMQ_URL: amqp://lehrerleicht:${RABBITMQ_PASSWORD:-devpassword}@rabbitmq:5672
      ANTHROPIC_API_KEY: ${ANTHROPIC_API_KEY}
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy
      rabbitmq:
        condition: service_healthy

  execution-service:
    build:
      context: ./services/execution-service
      dockerfile: Dockerfile
    container_name: lehrerleicht-execution
    environment:
      NODE_ENV: development
      REDIS_URL: redis://redis:6379
      POSTGRES_URL: postgres://lehrerleicht:${POSTGRES_PASSWORD:-devpassword}@postgres:5432/lehrerleicht
      RABBITMQ_URL: amqp://lehrerleicht:${RABBITMQ_PASSWORD:-devpassword}@rabbitmq:5672
      SCHOOLFOX_API_KEY: ${SCHOOLFOX_API_KEY:-}
      WEBUNTIS_USERNAME: ${WEBUNTIS_USERNAME:-}
      WEBUNTIS_PASSWORD: ${WEBUNTIS_PASSWORD:-}
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy
      rabbitmq:
        condition: service_healthy
      approval-service:
        condition: service_healthy

  # ============ FRONTEND ============
  web-frontend:
    build:
      context: ./services/web-frontend
      dockerfile: Dockerfile
    container_name: lehrerleicht-frontend
    environment:
      NODE_ENV: development
      VITE_API_URL: http://localhost:5000
      VITE_SIGNALR_URL: http://localhost:5000/hubs/approvals
    ports:
      - "3000:3000"
    depends_on:
      - approval-service

volumes:
  postgres_data:
  redis_data:
  rabbitmq_data:
```

### 10.2 Dockerfiles

#### C# Approval Service

```dockerfile
# services/approval-service/Dockerfile

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["src/Lehrerleicht.Approval.Api/Lehrerleicht.Approval.Api.csproj", "Lehrerleicht.Approval.Api/"]
COPY ["src/Lehrerleicht.Approval.Core/Lehrerleicht.Approval.Core.csproj", "Lehrerleicht.Approval.Core/"]
COPY ["src/Lehrerleicht.Approval.Infrastructure/Lehrerleicht.Approval.Infrastructure.csproj", "Lehrerleicht.Approval.Infrastructure/"]
RUN dotnet restore "Lehrerleicht.Approval.Api/Lehrerleicht.Approval.Api.csproj"

# Copy everything and build
COPY src/ .
RUN dotnet build "Lehrerleicht.Approval.Api/Lehrerleicht.Approval.Api.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "Lehrerleicht.Approval.Api/Lehrerleicht.Approval.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 5000
ENTRYPOINT ["dotnet", "Lehrerleicht.Approval.Api.dll"]
```

#### TypeScript Service (Example: AI Service)

```dockerfile
# services/ai-service/Dockerfile

FROM node:22-alpine AS builder
WORKDIR /app

# Copy package files
COPY package*.json ./
COPY tsconfig.json ./

# Install dependencies
RUN npm ci

# Copy source
COPY src/ ./src/

# Build
RUN npm run build

# Production image
FROM node:22-alpine
WORKDIR /app

# Copy built files and production dependencies
COPY --from=builder /app/dist ./dist
COPY --from=builder /app/package*.json ./
RUN npm ci --omit=dev

# Run as non-root
USER node

CMD ["node", "dist/index.js"]
```

#### SvelteKit Frontend

```dockerfile
# services/web-frontend/Dockerfile

FROM node:22-alpine AS builder
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM node:22-alpine
WORKDIR /app

COPY --from=builder /app/build ./build
COPY --from=builder /app/package*.json ./
RUN npm ci --omit=dev

USER node
EXPOSE 3000

CMD ["node", "build"]
```

---

## 11. Development Setup

### 11.1 Prerequisites

```bash
# Required software
- .NET 10 SDK
- Node.js 22 LTS
- Docker & Docker Compose
- pnpm (recommended) or npm
```

### 11.2 Quick Start

```bash
# 1. Clone the repository
git clone https://github.com/your-org/lehrerleicht.git
cd lehrerleicht

# 2. Copy environment file
cp .env.example .env
# Edit .env with your API keys

# 3. Start all services
docker-compose up -d

# 4. Run database migrations (C#)
cd services/approval-service
dotnet ef database update -p src/Lehrerleicht.Approval.Infrastructure -s src/Lehrerleicht.Approval.Api

# 5. Access the application
# Frontend: http://localhost:3000
# API: http://localhost:5000
# API Docs: http://localhost:5000/swagger
# RabbitMQ UI: http://localhost:15672
```

### 11.3 Environment Variables

```bash
# .env.example

# Database
POSTGRES_PASSWORD=devpassword

# RabbitMQ
RABBITMQ_PASSWORD=devpassword

# JWT
JWT_SECRET_KEY=your-super-secret-key-that-is-at-least-32-characters-long

# AI
ANTHROPIC_API_KEY=sk-ant-...

# External APIs (optional for development)
SCHOOLFOX_API_KEY=
WEBUNTIS_USERNAME=
WEBUNTIS_PASSWORD=
```

---

## 12. Team Endpoint Assignment

> **For University Project Grading**

| Team Member | Assigned Endpoints | Entities |
|-------------|-------------------|----------|
| **Member 1** | Auth (5), Teachers (4) | Teacher, School |
| **Member 2** | Approvals (6) | Approval, PendingAction, ActionOption |
| **Member 3** | Actions (3), Notifications (4) | ActionHistory, NotificationPreference |
| **Member 4** | Schools (4), Health (2), SignalR Hub | School (shared) |

### Endpoint Count Summary

| Category | Count |
|----------|-------|
| Auth | 5 |
| Teachers | 4 |
| Approvals | 6 |
| Actions | 3 |
| Notifications | 4 |
| Schools | 4 |
| Health | 2 |
| **Total REST** | **28** |
| SignalR Methods | 4 |

---

## Appendix A: Commands Reference

```bash
# C# Commands
dotnet new sln -n Lehrerleicht.ApprovalService
dotnet new webapi -n Lehrerleicht.Approval.Api
dotnet new classlib -n Lehrerleicht.Approval.Core
dotnet new classlib -n Lehrerleicht.Approval.Infrastructure
dotnet sln add **/*.csproj
dotnet ef migrations add InitialCreate -p src/Lehrerleicht.Approval.Infrastructure -s src/Lehrerleicht.Approval.Api
dotnet ef database update -p src/Lehrerleicht.Approval.Infrastructure -s src/Lehrerleicht.Approval.Api

# Node.js Commands
pnpm init
pnpm add typescript @types/node tsx -D
pnpm add @mastra/core @ai-sdk/anthropic bullmq amqplib

# Docker Commands
docker-compose up -d                    # Start all
docker-compose down                     # Stop all
docker-compose logs -f approval-service # Follow logs
docker-compose exec postgres psql -U lehrerleicht  # DB shell
```

---

**Document Version:** 1.0  
**Last Updated:** 2024-01-15  
**Authors:** Lehrerleicht Team
