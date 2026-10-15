-- ============================================================
-- RaceDay Database Schema
-- Matches Section A ERD (docs/ERD.png) exactly.
-- Target: Microsoft SQL Server
-- ============================================================

CREATE TABLE Users (
    UserID          INT             IDENTITY(1,1) PRIMARY KEY,
    FullName        VARCHAR(100)    NOT NULL,
    Email           VARCHAR(100)    NOT NULL UNIQUE,
    PasswordHash    VARCHAR(255)    NOT NULL,
    Role            VARCHAR(20)     NOT NULL CHECK (Role IN ('Organiser', 'Participant')),
    PhoneNumber     VARCHAR(20)     NULL,
    CreatedAt       DATETIME        NOT NULL DEFAULT GETDATE()
);

CREATE TABLE Venues (
    VenueID         INT             IDENTITY(1,1) PRIMARY KEY,
    VenueName       VARCHAR(100)    NOT NULL,
    Address         VARCHAR(255)    NOT NULL,
    City            VARCHAR(100)    NOT NULL,
    Province        VARCHAR(100)    NOT NULL,
    Latitude        DECIMAL(9,6)    NULL,
    Longitude       DECIMAL(9,6)    NULL
);

CREATE TABLE Events (
    EventID         INT             IDENTITY(1,1) PRIMARY KEY,
    OrganiserID     INT             NOT NULL,
    VenueID         INT             NOT NULL,
    EventName       VARCHAR(150)    NOT NULL,
    EventType       VARCHAR(50)     NOT NULL,   -- e.g. Marathon, Cycle Tour, Walk
    EventDate       DATE            NOT NULL,
    StartTime       TIME            NOT NULL,
    Description     VARCHAR(MAX)    NULL,
    Status          VARCHAR(20)     NOT NULL DEFAULT 'Scheduled',
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganiserID) REFERENCES Users(UserID),
    CONSTRAINT FK_Events_Venue     FOREIGN KEY (VenueID)     REFERENCES Venues(VenueID)
);

CREATE TABLE Categories (
    CategoryID      INT             IDENTITY(1,1) PRIMARY KEY,
    EventID         INT             NOT NULL,
    CategoryName    VARCHAR(50)     NOT NULL,   -- e.g. 10km, Half Marathon
    DistanceKM      DECIMAL(5,2)    NOT NULL,
    EntryFee        DECIMAL(8,2)    NOT NULL DEFAULT 0,
    MaxParticipants INT             NULL,
    CONSTRAINT FK_Categories_Event FOREIGN KEY (EventID) REFERENCES Events(EventID)
);

CREATE TABLE Registrations (
    RegistrationID    INT           IDENTITY(1,1) PRIMARY KEY,
    ParticipantID     INT           NOT NULL,
    CategoryID        INT           NOT NULL,
    BibNumber         VARCHAR(10)   NULL,
    RegistrationDate  DATETIME      NOT NULL DEFAULT GETDATE(),
    PaymentStatus     VARCHAR(20)   NOT NULL DEFAULT 'Pending',
    CONSTRAINT FK_Registrations_Participant FOREIGN KEY (ParticipantID) REFERENCES Users(UserID),
    CONSTRAINT FK_Registrations_Category    FOREIGN KEY (CategoryID)    REFERENCES Categories(CategoryID),
    CONSTRAINT UQ_Registrations_Participant_Category UNIQUE (ParticipantID, CategoryID)
);

CREATE TABLE Results (
    ResultID          INT           IDENTITY(1,1) PRIMARY KEY,
    RegistrationID    INT           NOT NULL UNIQUE,   -- enforces the 1:1 relationship
    FinishTime        TIME          NULL,
    OverallPosition   INT           NULL,
    CategoryPosition  INT           NULL,
    Status            VARCHAR(20)   NOT NULL DEFAULT 'Finished',  -- Finished, DNF, DQ
    CONSTRAINT FK_Results_Registration FOREIGN KEY (RegistrationID) REFERENCES Registrations(RegistrationID)
);
