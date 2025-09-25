using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Database.Providers.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Overview = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    ShortOverview = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Type = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ItemId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LogSeverity = table.Column<int>(type: "INTEGER", nullable: false),
                    RowVersion = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateLastActivity = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    AccessToken = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BaseItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<string>(type: "TEXT", nullable: true),
                    Path = table.Column<string>(type: "TEXT", nullable: true),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ChannelId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsMovie = table.Column<bool>(type: "INTEGER", nullable: false),
                    CommunityRating = table.Column<float>(type: "REAL", nullable: true),
                    CustomRating = table.Column<string>(type: "TEXT", nullable: true),
                    IndexNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    IsLocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    OfficialRating = table.Column<string>(type: "TEXT", nullable: true),
                    MediaType = table.Column<string>(type: "TEXT", nullable: true),
                    Overview = table.Column<string>(type: "TEXT", nullable: true),
                    ParentIndexNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    PremiereDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ProductionYear = table.Column<int>(type: "INTEGER", nullable: true),
                    Genres = table.Column<string>(type: "TEXT", nullable: true),
                    SortName = table.Column<string>(type: "TEXT", nullable: true),
                    ForcedSortName = table.Column<string>(type: "TEXT", nullable: true),
                    RunTimeTicks = table.Column<long>(type: "INTEGER", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DateModified = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsSeries = table.Column<bool>(type: "INTEGER", nullable: false),
                    EpisodeTitle = table.Column<string>(type: "TEXT", nullable: true),
                    IsRepeat = table.Column<bool>(type: "INTEGER", nullable: false),
                    PreferredMetadataLanguage = table.Column<string>(type: "TEXT", nullable: true),
                    PreferredMetadataCountryCode = table.Column<string>(type: "TEXT", nullable: true),
                    DateLastRefreshed = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DateLastSaved = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsInMixedFolder = table.Column<bool>(type: "INTEGER", nullable: false),
                    Studios = table.Column<string>(type: "TEXT", nullable: true),
                    ExternalServiceId = table.Column<string>(type: "TEXT", nullable: true),
                    Tags = table.Column<string>(type: "TEXT", nullable: true),
                    IsFolder = table.Column<bool>(type: "INTEGER", nullable: false),
                    InheritedParentalRatingValue = table.Column<int>(type: "INTEGER", nullable: true),
                    InheritedParentalRatingSubValue = table.Column<int>(type: "INTEGER", nullable: true),
                    UnratedType = table.Column<string>(type: "TEXT", nullable: true),
                    CriticRating = table.Column<float>(type: "REAL", nullable: true),
                    CleanName = table.Column<string>(type: "TEXT", nullable: true),
                    PresentationUniqueKey = table.Column<string>(type: "TEXT", nullable: true),
                    OriginalTitle = table.Column<string>(type: "TEXT", nullable: true),
                    PrimaryVersionId = table.Column<string>(type: "TEXT", nullable: true),
                    DateLastMediaAdded = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Album = table.Column<string>(type: "TEXT", nullable: true),
                    LUFS = table.Column<float>(type: "REAL", nullable: true),
                    NormalizationGain = table.Column<float>(type: "REAL", nullable: true),
                    IsVirtualItem = table.Column<bool>(type: "INTEGER", nullable: false),
                    SeriesName = table.Column<string>(type: "TEXT", nullable: true),
                    SeasonName = table.Column<string>(type: "TEXT", nullable: true),
                    ExternalSeriesId = table.Column<string>(type: "TEXT", nullable: true),
                    Tagline = table.Column<string>(type: "TEXT", nullable: true),
                    ProductionLocations = table.Column<string>(type: "TEXT", nullable: true),
                    ExtraIds = table.Column<string>(type: "TEXT", nullable: true),
                    TotalBitrate = table.Column<int>(type: "INTEGER", nullable: true),
                    ExtraType = table.Column<int>(type: "INTEGER", nullable: true),
                    Artists = table.Column<string>(type: "TEXT", nullable: true),
                    AlbumArtists = table.Column<string>(type: "TEXT", nullable: true),
                    ExternalId = table.Column<string>(type: "TEXT", nullable: true),
                    SeriesPresentationUniqueKey = table.Column<string>(type: "TEXT", nullable: true),
                    ShowId = table.Column<string>(type: "TEXT", nullable: true),
                    OwnerId = table.Column<string>(type: "TEXT", nullable: true),
                    Width = table.Column<int>(type: "INTEGER", nullable: true),
                    Height = table.Column<int>(type: "INTEGER", nullable: true),
                    Size = table.Column<long>(type: "INTEGER", nullable: true),
                    Audio = table.Column<int>(type: "INTEGER", nullable: true),
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TopParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SeasonId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SeriesId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomItemDisplayPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Client = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomItemDisplayPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeviceId = table.Column<string>(type: "TEXT", nullable: false),
                    CustomName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceOptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemValues",
                columns: table => new
                {
                    ItemValueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    CleanValue = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemValues", x => x.ItemValueId);
                });

            migrationBuilder.CreateTable(
                name: "MediaSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    EndTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    StartTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    SegmentProviderId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaSegments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Peoples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    PersonType = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Peoples", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PinBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    SubscriptionType = table.Column<int>(type: "INTEGER", nullable: false),
                    PinPattern = table.Column<int>(type: "INTEGER", nullable: false),
                    PinLength = table.Column<int>(type: "INTEGER", nullable: false),
                    CustomCharacterSet = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    TotalPins = table.Column<int>(type: "INTEGER", nullable: false),
                    UsedPins = table.Column<int>(type: "INTEGER", nullable: false),
                    ActivePins = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpiredPins = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    MaxConcurrentSessions = table.Column<int>(type: "INTEGER", nullable: true),
                    AllowRemoteAccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxBitrate = table.Column<int>(type: "INTEGER", nullable: true),
                    AllowTranscoding = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxParentalRating = table.Column<int>(type: "INTEGER", nullable: true),
                    AllowDownload = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowSyncPlay = table.Column<bool>(type: "INTEGER", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", nullable: true),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    Metadata = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PinBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SubscriptionType = table.Column<int>(type: "INTEGER", nullable: false),
                    CustomDurationHours = table.Column<int>(type: "INTEGER", nullable: true),
                    MaxConcurrentSessions = table.Column<int>(type: "INTEGER", nullable: false),
                    AllowRemoteAccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxBitrate = table.Column<int>(type: "INTEGER", nullable: true),
                    AllowTranscoding = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxParentalRating = table.Column<int>(type: "INTEGER", nullable: true),
                    AllowDownload = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowSyncPlay = table.Column<bool>(type: "INTEGER", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrickplayInfos",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: false),
                    Height = table.Column<int>(type: "INTEGER", nullable: false),
                    TileWidth = table.Column<int>(type: "INTEGER", nullable: false),
                    TileHeight = table.Column<int>(type: "INTEGER", nullable: false),
                    ThumbnailCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Interval = table.Column<int>(type: "INTEGER", nullable: false),
                    Bandwidth = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrickplayInfos", x => new { x.ItemId, x.Width });
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Password = table.Column<string>(type: "TEXT", maxLength: 65535, nullable: true),
                    PinCode = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    SubscriptionType = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MustUpdatePassword = table.Column<bool>(type: "INTEGER", nullable: false),
                    AudioLanguagePreference = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    AuthenticationProviderId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    PasswordResetProviderId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    InvalidLoginAttemptCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastActivityDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastLoginDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LoginAttemptsBeforeLockout = table.Column<int>(type: "INTEGER", nullable: true),
                    MaxActiveSessions = table.Column<int>(type: "INTEGER", nullable: false),
                    SubtitleMode = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayDefaultAudioTrack = table.Column<bool>(type: "INTEGER", nullable: false),
                    SubtitleLanguagePreference = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    DisplayMissingEpisodes = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayCollectionsView = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnableLocalPassword = table.Column<bool>(type: "INTEGER", nullable: false),
                    HidePlayedInLatest = table.Column<bool>(type: "INTEGER", nullable: false),
                    RememberAudioSelections = table.Column<bool>(type: "INTEGER", nullable: false),
                    RememberSubtitleSelections = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnableNextEpisodeAutoPlay = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnableAutoLogin = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnableUserPreferenceAccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxParentalRatingScore = table.Column<int>(type: "INTEGER", nullable: true),
                    MaxParentalRatingSubScore = table.Column<int>(type: "INTEGER", nullable: true),
                    RemoteClientBitrateLimit = table.Column<int>(type: "INTEGER", nullable: true),
                    InternalId = table.Column<long>(type: "INTEGER", nullable: false),
                    SyncPlayAccess = table.Column<int>(type: "INTEGER", nullable: false),
                    CastReceiverId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    RowVersion = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AncestorIds",
                columns: table => new
                {
                    ParentItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AncestorIds", x => new { x.ItemId, x.ParentItemId });
                    table.ForeignKey(
                        name: "FK_AncestorIds_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AncestorIds_BaseItems_ParentItemId",
                        column: x => x.ParentItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AttachmentStreamInfos",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    Codec = table.Column<string>(type: "TEXT", nullable: true),
                    CodecTag = table.Column<string>(type: "TEXT", nullable: true),
                    Comment = table.Column<string>(type: "TEXT", nullable: true),
                    Filename = table.Column<string>(type: "TEXT", nullable: true),
                    MimeType = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttachmentStreamInfos", x => new { x.ItemId, x.Index });
                    table.ForeignKey(
                        name: "FK_AttachmentStreamInfos_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BaseItemImageInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    DateModified = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ImageType = table.Column<int>(type: "INTEGER", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: false),
                    Height = table.Column<int>(type: "INTEGER", nullable: false),
                    Blurhash = table.Column<byte[]>(type: "BLOB", nullable: true),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseItemImageInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaseItemImageInfos_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BaseItemMetadataFields",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseItemMetadataFields", x => new { x.Id, x.ItemId });
                    table.ForeignKey(
                        name: "FK_BaseItemMetadataFields_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BaseItemProviders",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProviderId = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderValue = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseItemProviders", x => new { x.ItemId, x.ProviderId });
                    table.ForeignKey(
                        name: "FK_BaseItemProviders_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BaseItemTrailerTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseItemTrailerTypes", x => new { x.Id, x.ItemId });
                    table.ForeignKey(
                        name: "FK_BaseItemTrailerTypes_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Chapters",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChapterIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    StartPositionTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    ImagePath = table.Column<string>(type: "TEXT", nullable: true),
                    ImageDateModified = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chapters", x => new { x.ItemId, x.ChapterIndex });
                    table.ForeignKey(
                        name: "FK_Chapters_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KeyframeData",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TotalDuration = table.Column<long>(type: "INTEGER", nullable: false),
                    KeyframeTicks = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyframeData", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_KeyframeData_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MediaStreamInfos",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StreamIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    StreamType = table.Column<int>(type: "INTEGER", nullable: false),
                    Codec = table.Column<string>(type: "TEXT", nullable: true),
                    Language = table.Column<string>(type: "TEXT", nullable: true),
                    ChannelLayout = table.Column<string>(type: "TEXT", nullable: true),
                    Profile = table.Column<string>(type: "TEXT", nullable: true),
                    AspectRatio = table.Column<string>(type: "TEXT", nullable: true),
                    Path = table.Column<string>(type: "TEXT", nullable: true),
                    IsInterlaced = table.Column<bool>(type: "INTEGER", nullable: true),
                    BitRate = table.Column<int>(type: "INTEGER", nullable: true),
                    Channels = table.Column<int>(type: "INTEGER", nullable: true),
                    SampleRate = table.Column<int>(type: "INTEGER", nullable: true),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsForced = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsExternal = table.Column<bool>(type: "INTEGER", nullable: false),
                    Height = table.Column<int>(type: "INTEGER", nullable: true),
                    Width = table.Column<int>(type: "INTEGER", nullable: true),
                    AverageFrameRate = table.Column<float>(type: "REAL", nullable: true),
                    RealFrameRate = table.Column<float>(type: "REAL", nullable: true),
                    Level = table.Column<float>(type: "REAL", nullable: true),
                    PixelFormat = table.Column<string>(type: "TEXT", nullable: true),
                    BitDepth = table.Column<int>(type: "INTEGER", nullable: true),
                    IsAnamorphic = table.Column<bool>(type: "INTEGER", nullable: true),
                    RefFrames = table.Column<int>(type: "INTEGER", nullable: true),
                    CodecTag = table.Column<string>(type: "TEXT", nullable: true),
                    Comment = table.Column<string>(type: "TEXT", nullable: true),
                    NalLengthSize = table.Column<string>(type: "TEXT", nullable: true),
                    IsAvc = table.Column<bool>(type: "INTEGER", nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    TimeBase = table.Column<string>(type: "TEXT", nullable: true),
                    CodecTimeBase = table.Column<string>(type: "TEXT", nullable: true),
                    ColorPrimaries = table.Column<string>(type: "TEXT", nullable: true),
                    ColorSpace = table.Column<string>(type: "TEXT", nullable: true),
                    ColorTransfer = table.Column<string>(type: "TEXT", nullable: true),
                    DvVersionMajor = table.Column<int>(type: "INTEGER", nullable: true),
                    DvVersionMinor = table.Column<int>(type: "INTEGER", nullable: true),
                    DvProfile = table.Column<int>(type: "INTEGER", nullable: true),
                    DvLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    RpuPresentFlag = table.Column<int>(type: "INTEGER", nullable: true),
                    ElPresentFlag = table.Column<int>(type: "INTEGER", nullable: true),
                    BlPresentFlag = table.Column<int>(type: "INTEGER", nullable: true),
                    DvBlSignalCompatibilityId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsHearingImpaired = table.Column<bool>(type: "INTEGER", nullable: true),
                    Rotation = table.Column<int>(type: "INTEGER", nullable: true),
                    KeyFrames = table.Column<string>(type: "TEXT", nullable: true),
                    Hdr10PlusPresentFlag = table.Column<bool>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaStreamInfos", x => new { x.ItemId, x.StreamIndex });
                    table.ForeignKey(
                        name: "FK_MediaStreamInfos_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemValuesMap",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ItemValueId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemValuesMap", x => new { x.ItemValueId, x.ItemId });
                    table.ForeignKey(
                        name: "FK_ItemValuesMap_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemValuesMap_ItemValues_ItemValueId",
                        column: x => x.ItemValueId,
                        principalTable: "ItemValues",
                        principalColumn: "ItemValueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PeopleBaseItemMap",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PeopleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: true),
                    ListOrder = table.Column<int>(type: "INTEGER", nullable: true),
                    Role = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeopleBaseItemMap", x => new { x.ItemId, x.PeopleId });
                    table.ForeignKey(
                        name: "FK_PeopleBaseItemMap_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PeopleBaseItemMap_Peoples_PeopleId",
                        column: x => x.PeopleId,
                        principalTable: "Peoples",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccessSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DayOfWeek = table.Column<int>(type: "INTEGER", nullable: false),
                    StartHour = table.Column<double>(type: "REAL", nullable: false),
                    EndHour = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessSchedules_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccessToken = table.Column<string>(type: "TEXT", nullable: false),
                    AppName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    AppVersion = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    DeviceName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DeviceId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateLastActivity = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Devices_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DisplayPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Client = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ShowSidebar = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowBackdrop = table.Column<bool>(type: "INTEGER", nullable: false),
                    ScrollDirection = table.Column<int>(type: "INTEGER", nullable: false),
                    IndexBy = table.Column<int>(type: "INTEGER", nullable: true),
                    SkipForwardLength = table.Column<int>(type: "INTEGER", nullable: false),
                    SkipBackwardLength = table.Column<int>(type: "INTEGER", nullable: false),
                    ChromecastVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    EnableNextVideoInfoOverlay = table.Column<bool>(type: "INTEGER", nullable: false),
                    DashboardTheme = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    TvHome = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisplayPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisplayPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImageInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Path = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImageInfos_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemDisplayPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Client = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ViewType = table.Column<int>(type: "INTEGER", nullable: false),
                    RememberIndexing = table.Column<bool>(type: "INTEGER", nullable: false),
                    IndexBy = table.Column<int>(type: "INTEGER", nullable: true),
                    RememberSorting = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortBy = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemDisplayPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemDisplayPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<bool>(type: "INTEGER", nullable: false),
                    RowVersion = table.Column<uint>(type: "INTEGER", nullable: false),
                    Permission_Permissions_Guid = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PinBatchUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BatchId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PinCode = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    OriginalPin = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FirstUsedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastUsedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeactivatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeactivationReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    LastLoginIp = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    LastLoginDevice = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PinBatchUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PinBatchUsers_PinBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "PinBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PinBatchUsers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Preferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 65535, nullable: false),
                    RowVersion = table.Column<uint>(type: "INTEGER", nullable: false),
                    Preference_Preferences_Guid = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Preferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Preferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserData",
                columns: table => new
                {
                    CustomDataKey = table.Column<string>(type: "TEXT", nullable: false),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Rating = table.Column<double>(type: "REAL", nullable: true),
                    PlaybackPositionTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    PlayCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastPlayedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Played = table.Column<bool>(type: "INTEGER", nullable: false),
                    AudioStreamIndex = table.Column<int>(type: "INTEGER", nullable: true),
                    SubtitleStreamIndex = table.Column<int>(type: "INTEGER", nullable: true),
                    Likes = table.Column<bool>(type: "INTEGER", nullable: true),
                    RetentionDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserData", x => new { x.ItemId, x.UserId, x.CustomDataKey });
                    table.ForeignKey(
                        name: "FK_UserData_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserData_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HomeSection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DisplayPreferencesId = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeSection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HomeSection_DisplayPreferences_DisplayPreferencesId",
                        column: x => x.DisplayPreferencesId,
                        principalTable: "DisplayPreferences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "BaseItems",
                columns: new[] { "Id", "Album", "AlbumArtists", "Artists", "Audio", "ChannelId", "CleanName", "CommunityRating", "CriticRating", "CustomRating", "Data", "DateCreated", "DateLastMediaAdded", "DateLastRefreshed", "DateLastSaved", "DateModified", "EndDate", "EpisodeTitle", "ExternalId", "ExternalSeriesId", "ExternalServiceId", "ExtraIds", "ExtraType", "ForcedSortName", "Genres", "Height", "IndexNumber", "InheritedParentalRatingSubValue", "InheritedParentalRatingValue", "IsFolder", "IsInMixedFolder", "IsLocked", "IsMovie", "IsRepeat", "IsSeries", "IsVirtualItem", "LUFS", "MediaType", "Name", "NormalizationGain", "OfficialRating", "OriginalTitle", "Overview", "OwnerId", "ParentId", "ParentIndexNumber", "Path", "PreferredMetadataCountryCode", "PreferredMetadataLanguage", "PremiereDate", "PresentationUniqueKey", "PrimaryVersionId", "ProductionLocations", "ProductionYear", "RunTimeTicks", "SeasonId", "SeasonName", "SeriesId", "SeriesName", "SeriesPresentationUniqueKey", "ShowId", "Size", "SortName", "StartDate", "Studios", "Tagline", "Tags", "TopParentId", "TotalBitrate", "Type", "UnratedType", "Width" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, false, false, false, null, null, "This is a placeholder item for UserData that has been detacted from its original item", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "PLACEHOLDER", null, null });

            migrationBuilder.CreateIndex(
                name: "IX_AccessSchedules_UserId",
                table: "AccessSchedules",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_DateCreated",
                table: "ActivityLogs",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_AncestorIds_ParentItemId",
                table: "AncestorIds",
                column: "ParentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_AccessToken",
                table: "ApiKeys",
                column: "AccessToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BaseItemImageInfos_ItemId",
                table: "BaseItemImageInfos",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItemMetadataFields_ItemId",
                table: "BaseItemMetadataFields",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItemProviders_ProviderId_ProviderValue_ItemId",
                table: "BaseItemProviders",
                columns: new[] { "ProviderId", "ProviderValue", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Id_Type_IsFolder_IsVirtualItem",
                table: "BaseItems",
                columns: new[] { "Id", "Type", "IsFolder", "IsVirtualItem" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_IsFolder_TopParentId_IsVirtualItem_PresentationUniqueKey_DateCreated",
                table: "BaseItems",
                columns: new[] { "IsFolder", "TopParentId", "IsVirtualItem", "PresentationUniqueKey", "DateCreated" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_MediaType_TopParentId_IsVirtualItem_PresentationUniqueKey",
                table: "BaseItems",
                columns: new[] { "MediaType", "TopParentId", "IsVirtualItem", "PresentationUniqueKey" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_ParentId",
                table: "BaseItems",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Path",
                table: "BaseItems",
                column: "Path");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_PresentationUniqueKey",
                table: "BaseItems",
                column: "PresentationUniqueKey");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_TopParentId_Id",
                table: "BaseItems",
                columns: new[] { "TopParentId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_SeriesPresentationUniqueKey_IsFolder_IsVirtualItem",
                table: "BaseItems",
                columns: new[] { "Type", "SeriesPresentationUniqueKey", "IsFolder", "IsVirtualItem" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_SeriesPresentationUniqueKey_PresentationUniqueKey_SortName",
                table: "BaseItems",
                columns: new[] { "Type", "SeriesPresentationUniqueKey", "PresentationUniqueKey", "SortName" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_TopParentId_Id",
                table: "BaseItems",
                columns: new[] { "Type", "TopParentId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_TopParentId_IsVirtualItem_PresentationUniqueKey_DateCreated",
                table: "BaseItems",
                columns: new[] { "Type", "TopParentId", "IsVirtualItem", "PresentationUniqueKey", "DateCreated" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_TopParentId_PresentationUniqueKey",
                table: "BaseItems",
                columns: new[] { "Type", "TopParentId", "PresentationUniqueKey" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_TopParentId_StartDate",
                table: "BaseItems",
                columns: new[] { "Type", "TopParentId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItemTrailerTypes_ItemId",
                table: "BaseItemTrailerTypes",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomItemDisplayPreferences_UserId_ItemId_Client_Key",
                table: "CustomItemDisplayPreferences",
                columns: new[] { "UserId", "ItemId", "Client", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceOptions_DeviceId",
                table: "DeviceOptions",
                column: "DeviceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_AccessToken_DateLastActivity",
                table: "Devices",
                columns: new[] { "AccessToken", "DateLastActivity" });

            migrationBuilder.CreateIndex(
                name: "IX_Devices_DeviceId",
                table: "Devices",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_DeviceId_DateLastActivity",
                table: "Devices",
                columns: new[] { "DeviceId", "DateLastActivity" });

            migrationBuilder.CreateIndex(
                name: "IX_Devices_UserId_DeviceId",
                table: "Devices",
                columns: new[] { "UserId", "DeviceId" });

            migrationBuilder.CreateIndex(
                name: "IX_DisplayPreferences_UserId_ItemId_Client",
                table: "DisplayPreferences",
                columns: new[] { "UserId", "ItemId", "Client" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HomeSection_DisplayPreferencesId",
                table: "HomeSection",
                column: "DisplayPreferencesId");

            migrationBuilder.CreateIndex(
                name: "IX_ImageInfos_UserId",
                table: "ImageInfos",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemDisplayPreferences_UserId",
                table: "ItemDisplayPreferences",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemValues_Type_CleanValue",
                table: "ItemValues",
                columns: new[] { "Type", "CleanValue" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemValues_Type_Value",
                table: "ItemValues",
                columns: new[] { "Type", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemValuesMap_ItemId",
                table: "ItemValuesMap",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaStreamInfos_StreamIndex",
                table: "MediaStreamInfos",
                column: "StreamIndex");

            migrationBuilder.CreateIndex(
                name: "IX_MediaStreamInfos_StreamIndex_StreamType",
                table: "MediaStreamInfos",
                columns: new[] { "StreamIndex", "StreamType" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaStreamInfos_StreamIndex_StreamType_Language",
                table: "MediaStreamInfos",
                columns: new[] { "StreamIndex", "StreamType", "Language" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaStreamInfos_StreamType",
                table: "MediaStreamInfos",
                column: "StreamType");

            migrationBuilder.CreateIndex(
                name: "IX_PeopleBaseItemMap_ItemId_ListOrder",
                table: "PeopleBaseItemMap",
                columns: new[] { "ItemId", "ListOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PeopleBaseItemMap_ItemId_SortOrder",
                table: "PeopleBaseItemMap",
                columns: new[] { "ItemId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PeopleBaseItemMap_PeopleId",
                table: "PeopleBaseItemMap",
                column: "PeopleId");

            migrationBuilder.CreateIndex(
                name: "IX_Peoples_Name",
                table: "Peoples",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_UserId_Kind",
                table: "Permissions",
                columns: new[] { "UserId", "Kind" },
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PinBatchUsers_BatchId",
                table: "PinBatchUsers",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PinBatchUsers_UserId",
                table: "PinBatchUsers",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Preferences_UserId_Kind",
                table: "Preferences",
                columns: new[] { "UserId", "Kind" },
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionConfigurations_CreatedDate",
                table: "SubscriptionConfigurations",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionConfigurations_IsActive",
                table: "SubscriptionConfigurations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionConfigurations_SubscriptionType",
                table: "SubscriptionConfigurations",
                column: "SubscriptionType");

            migrationBuilder.CreateIndex(
                name: "IX_UserData_ItemId_UserId_IsFavorite",
                table: "UserData",
                columns: new[] { "ItemId", "UserId", "IsFavorite" });

            migrationBuilder.CreateIndex(
                name: "IX_UserData_ItemId_UserId_LastPlayedDate",
                table: "UserData",
                columns: new[] { "ItemId", "UserId", "LastPlayedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_UserData_ItemId_UserId_PlaybackPositionTicks",
                table: "UserData",
                columns: new[] { "ItemId", "UserId", "PlaybackPositionTicks" });

            migrationBuilder.CreateIndex(
                name: "IX_UserData_ItemId_UserId_Played",
                table: "UserData",
                columns: new[] { "ItemId", "UserId", "Played" });

            migrationBuilder.CreateIndex(
                name: "IX_UserData_UserId",
                table: "UserData",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessSchedules");

            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropTable(
                name: "AncestorIds");

            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "AttachmentStreamInfos");

            migrationBuilder.DropTable(
                name: "BaseItemImageInfos");

            migrationBuilder.DropTable(
                name: "BaseItemMetadataFields");

            migrationBuilder.DropTable(
                name: "BaseItemProviders");

            migrationBuilder.DropTable(
                name: "BaseItemTrailerTypes");

            migrationBuilder.DropTable(
                name: "Chapters");

            migrationBuilder.DropTable(
                name: "CustomItemDisplayPreferences");

            migrationBuilder.DropTable(
                name: "DeviceOptions");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropTable(
                name: "HomeSection");

            migrationBuilder.DropTable(
                name: "ImageInfos");

            migrationBuilder.DropTable(
                name: "ItemDisplayPreferences");

            migrationBuilder.DropTable(
                name: "ItemValuesMap");

            migrationBuilder.DropTable(
                name: "KeyframeData");

            migrationBuilder.DropTable(
                name: "MediaSegments");

            migrationBuilder.DropTable(
                name: "MediaStreamInfos");

            migrationBuilder.DropTable(
                name: "PeopleBaseItemMap");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "PinBatchUsers");

            migrationBuilder.DropTable(
                name: "Preferences");

            migrationBuilder.DropTable(
                name: "SubscriptionConfigurations");

            migrationBuilder.DropTable(
                name: "TrickplayInfos");

            migrationBuilder.DropTable(
                name: "UserData");

            migrationBuilder.DropTable(
                name: "DisplayPreferences");

            migrationBuilder.DropTable(
                name: "ItemValues");

            migrationBuilder.DropTable(
                name: "Peoples");

            migrationBuilder.DropTable(
                name: "PinBatches");

            migrationBuilder.DropTable(
                name: "BaseItems");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
