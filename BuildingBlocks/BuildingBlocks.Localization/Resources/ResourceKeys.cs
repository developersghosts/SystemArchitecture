namespace BuildingBlocks.Localization.Resources;

public static class ResourceKeys
{
    // General
    public const string APP_Name = "APP_Name";
    public const string NotFound = "NotFound";
    public const string BadRequest = "BadRequest";
    public const string ValidationFailed = "ValidationFailed";
    public const string InternalServerError = "InternalServerError";
    public const string Unauthorized = "Unauthorized";
    public const string TooManyRequests = "TooManyRequests";
    public const string AllRightsReserved = "AllRightsReserved";

    // Login / OTP / session
    public const string FailedLogin = "FailedLogin";
    public const string LoginSuccessful = "LoginSuccessful";
    public const string SuccessLoginTitle = "SuccessLoginTitle";
    public const string IFYouNotLoggedIn = "IFYouNotLoggedIn";
    public const string Login_Information_Incorrect = "Login_Information_Incorrect";
    public const string UserNotActive = "UserNotActive";
    public const string AccountLocked = "AccountLocked";
    public const string User_Already_Logged_In = "User_Already_Logged_In";
    public const string OTP_Sent = "OTP_Sent";
    public const string OTP_FAIL = "OTP_FAIL";
    public const string OtpEmailTitle = "OtpEmailTitle";
    public const string OtpEmailBody = "OtpEmailBody";
    public const string CurrentUserUnknown = "CurrentUserUnknown";

    // Users / roles
    public const string UserNotFound = "UserNotFound";
    public const string RoleNotFound = "RoleNotFound";
    public const string RolesAlreadyAssigned = "RolesAlreadyAssigned";
    public const string CannotRemoveOwnAdministrator = "CannotRemoveOwnAdministrator";
    public const string CannotDeactivateYourself = "CannotDeactivateYourself";
    public const string SessionCannotBeRenewed = "SessionCannotBeRenewed";
    public const string SmtpPasswordRequired = "SmtpPasswordRequired";
    public const string EmailButtonFallback = "EmailButtonFallback";
    public const string EmailAutomatedNote = "EmailAutomatedNote";
    public const string GroupNotFound = "GroupNotFound";
    public const string GroupNameExists = "GroupNameExists";
    public const string CannotLoseOwnAdministratorByGroup = "CannotLoseOwnAdministratorByGroup";
    public const string TestEmailTitle = "TestEmailTitle";
    public const string TestEmailBody = "TestEmailBody";
    public const string WelcomeTitle = "WelcomeTitle";
    public const string WelcomeBody = "WelcomeBody";

    // Passwords
    public const string PasswordChangedTitle = "PasswordChangedTitle";
    public const string PasswordChangedBody = "PasswordChangedBody";
    public const string ResetPasswordTitle = "ResetPasswordTitle";
    public const string ResetPasswordBody = "ResetPasswordBody";
    public const string ResetPasswordButton = "ResetPasswordButton";
    public const string InvalidResetToken = "InvalidResetToken";
}
