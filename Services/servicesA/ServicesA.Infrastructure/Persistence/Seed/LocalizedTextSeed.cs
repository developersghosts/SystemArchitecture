namespace ServicesA.Infrastructure.Persistence.Seed;

/// <summary>
/// The starting message dictionary: English, Egyptian Arabic and Saudi Arabic for every message key.
/// Added at startup only where a row is missing, so texts edited in Settings > Message dictionary are kept.
/// </summary>
public static class LocalizedTextSeed
{
    public sealed record Entry(string Key, string En, string Egyptian, string Saudi);

    public static readonly Entry[] Entries =
    [
        // General
        new("NotFound", "The requested item was not found.",
            "الحاجة اللي بتدور عليها مش موجودة.", "الشي اللي تدوّر عليه مو موجود."),
        new("BadRequest", "The request is invalid.",
            "الطلب ده مش صحيح.", "هالطلب غير صحيح."),
        new("ValidationFailed", "Some of the data is not valid. Please check it.",
            "فيه بيانات مش مظبوطة، راجعها تاني.", "فيه بيانات مو مضبوطة، راجعها مرة ثانية."),
        new("InternalServerError", "An unexpected error occurred. Please try again later.",
            "حصلت مشكلة مش متوقعة، جرّب تاني بعد شوية.", "صار خطأ غير متوقع، جرّب مرة ثانية بعد شوي."),
        new("Unauthorized", "You are not allowed to do this.",
            "مش مسموحلك تعمل الحاجة دي.", "ما عندك صلاحية تسوي هالشي."),
        new("TooManyRequests", "Too many attempts. Please wait a moment and try again.",
            "محاولات كتير أوي، استنى شوية وجرّب تاني.", "محاولات كثيرة، انتظر شوي وجرّب مرة ثانية."),
        new("AllRightsReserved", "All rights reserved",
            "كل الحقوق محفوظة", "جميع الحقوق محفوظة"),

        // Sign in
        new("FailedLogin", "Sign-in failed.",
            "الدخول ما نجحش.", "ما قدرنا نسجّل دخولك."),
        new("LoginSuccessful", "Signed in successfully.",
            "دخلت بنجاح.", "تم تسجيل دخولك بنجاح."),
        new("SuccessLoginTitle", "New sign-in to your account",
            "حد دخل على حسابك دلوقتي", "تسجيل دخول جديد على حسابك"),
        new("IFYouNotLoggedIn", "If this was not you, change your password and contact support right away.",
            "لو مش إنت اللي دخلت، غيّر الباسورد وكلّم الدعم على طول.",
            "إذا ما كنت أنت اللي دخلت، غيّر كلمة المرور وتواصل مع الدعم على طول."),
        new("Login_Information_Incorrect", "The user name or password is incorrect.",
            "اسم المستخدم أو الباسورد غلط.", "اسم المستخدم أو كلمة المرور غلط."),
        new("UserNotActive", "This account is deactivated. Please contact the administrator.",
            "الحساب ده متوقف، كلّم مدير السيستم.", "هالحساب موقوف، تواصل مع مدير النظام."),
        new("AccountLocked", "The account is locked for a while after too many wrong attempts. Try again later.",
            "الحساب اتقفل شوية بسبب محاولات غلط كتير، جرّب تاني بعدين.",
            "انقفل الحساب مؤقتاً بسبب محاولات خاطئة كثيرة، جرّب بعدين."),
        new("User_Already_Logged_In", "This account is signed in on another device. Sign out there, or sign in here and end the other session.",
            "الحساب ده مفتوح على جهاز تاني. اخرج منه هناك، أو ادخل من هنا واقفل الجلسة التانية.",
            "هالحساب مفتوح من جهاز ثاني. اطلع منه هناك، أو ادخل من هنا وسكّر الجلسة الثانية."),
        new("CurrentUserUnknown", "We could not identify you. Please sign in again.",
            "مش عارفين إنت مين، سجّل دخول تاني.", "ما قدرنا نتعرف عليك، سجّل دخولك مرة ثانية."),
        new("SessionCannotBeRenewed", "Your session has ended and cannot be renewed. Please sign in again.",
            "الجلسة خلصت ومينفعش تتجدد، سجّل دخول تاني.", "انتهت الجلسة وما تقدر تجددها، سجّل دخولك مرة ثانية."),

        // Verification code
        new("OTP_Sent", "We sent a verification code to your email.",
            "بعتنالك كود التحقق على الإيميل.", "أرسلنا لك رمز التحقق على إيميلك."),
        new("OTP_FAIL", "The verification code is wrong or has expired.",
            "الكود غلط أو خلص وقته.", "الرمز غلط أو انتهت صلاحيته."),
        new("OtpEmailTitle", "Your verification code",
            "كود التحقق بتاعك", "رمز التحقق حقك"),
        new("OtpEmailBody", "Use this code to finish signing in. It works for 5 minutes.",
            "استخدم الكود ده عشان تكمّل الدخول، وهيفضل شغال 5 دقايق.",
            "استخدم هالرمز عشان تكمّل تسجيل الدخول، وصلاحيته 5 دقايق."),

        // Users and roles
        new("UserNotFound", "The user was not found.",
            "المستخدم ده مش موجود.", "هالمستخدم مو موجود."),
        new("RoleNotFound", "One or more roles were not found.",
            "فيه صلاحية أو أكتر مش موجودة.", "فيه صلاحية أو أكثر مو موجودة."),
        new("RolesAlreadyAssigned", "These roles are already given to the user:",
            "الصلاحيات دي متضافة للمستخدم من قبل:", "هذي الصلاحيات مضافة للمستخدم من قبل:"),
        new("CannotRemoveOwnAdministrator", "You cannot remove the Administrator role from your own account.",
            "مينفعش تشيل صلاحية المدير من حسابك.", "ما ينفع تشيل صلاحية المدير من حسابك."),
        new("CannotDeactivateYourself", "You cannot deactivate your own account.",
            "مينفعش توقف حسابك إنت.", "ما ينفع توقف حسابك."),
        new("GroupNotFound", "The group was not found.",
            "المجموعة دي مش موجودة.", "هالمجموعة مو موجودة."),
        new("GroupNameExists", "A group with this name already exists.",
            "فيه مجموعة بنفس الاسم ده.", "فيه مجموعة بنفس الاسم."),
        new("CannotLoseOwnAdministratorByGroup", "This change would remove the Administrator role from your own account.",
            "التعديل ده هيشيل صلاحية المدير من حسابك.", "هالتعديل بيشيل صلاحية المدير من حسابك."),

        // Emails
        new("EmailButtonFallback", "If the button does not work, copy this link into your browser:",
            "لو الزرار مش شغال، انسخ اللينك ده وافتحه في المتصفح:", "إذا الزر ما اشتغل، انسخ هالرابط وافتحه في المتصفح:"),
        new("EmailAutomatedNote", "This is an automatic message, please do not reply to it.",
            "الرسالة دي اتبعتت أوتوماتيك، متردش عليها.", "هذي رسالة تلقائية، لا ترد عليها."),
        new("SmtpPasswordRequired", "You changed the server or user name: enter the SMTP password again.",
            "غيّرت السيرفر أو اسم المستخدم، اكتب باسورد الـ SMTP تاني.",
            "غيّرت السيرفر أو اسم المستخدم، اكتب كلمة مرور الـ SMTP مرة ثانية."),
        new("TestEmailTitle", "Test email",
            "إيميل تجريبي", "رسالة تجريبية"),
        new("TestEmailBody", "If you can read this, the email settings work.",
            "لو بتقرا الرسالة دي يبقى إعدادات الإيميل شغالة تمام.", "إذا تقرأ هالرسالة فإعدادات الإيميل شغّالة تمام."),
        new("WelcomeTitle", "Welcome",
            "أهلاً بيك", "هلا وغلا"),
        new("WelcomeBody", "Your account is ready. Your user name is:",
            "عملنالك حساب جديد، واسم المستخدم بتاعك:", "أنشأنا لك حساب جديد، واسم المستخدم حقك:"),
        new("PasswordChangedTitle", "Your password was changed",
            "الباسورد اتغيّر", "تم تغيير كلمة المرور"),
        new("PasswordChangedBody", "The password of your account was changed and all previous sessions were signed out.",
            "باسورد حسابك اتغيّر، وقفلنا كل الجلسات القديمة.", "تغيّرت كلمة مرور حسابك، وسكّرنا كل الجلسات السابقة."),
        new("ResetPasswordTitle", "Reset your password",
            "تغيير الباسورد", "استعادة كلمة المرور"),
        new("ResetPasswordBody", "We received a request to reset your password. If you did not ask for it, ignore this email.",
            "وصلنا طلب إنك تغيّر الباسورد. لو مش إنت اللي طلبته، تجاهل الإيميل ده.",
            "وصلنا طلب لاستعادة كلمة المرور. إذا ما طلبته أنت، تجاهل هالإيميل."),
        new("ResetPasswordButton", "Reset password",
            "غيّر الباسورد", "استعادة كلمة المرور"),
        new("InvalidResetToken", "The reset link is wrong or has expired.",
            "اللينك ده غلط أو خلص وقته.", "الرابط غلط أو انتهت صلاحيته.")
    ];
}
