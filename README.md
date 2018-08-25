# Two Factor Authentication using Google Authenticator and ASP.NET MVC Identity

Full sample to create ASP.NET MVC with Individual User Account Authentication and Two Factor Authentication using Google Authenticator.

# Steps:

1- Add new MVC Project
![alt tag](https://image.prntscr.com/image/JPtChf7QT7_LDuf6vUbfEw.png)
![alt tag](https://image.prntscr.com/image/WwnM0XsFQpaQW1hPGctM3g.png)
![alt tag](https://image.prntscr.com/image/JsfZAFscSvaqzh6e5Kqnxg.png)

2- Install Google Authenticator using NuGet Package Manager
After creating project, go to solution name, right click, then NuGet Package Manager
![alt tag](https://image.prntscr.com/image/6xcFFFjvSYOdAMoqizVYmg.png)

3- Customize Identity Properties, add two columns to AspNetUsers that will created next, by adding these properties to ApplicationUser class in IdentityModels.cs
````csharp
        public bool TwoFactorGoogleEnabled { get; set; }
        public string TwoFactorGoogleCode { get; set; }
````
![alt tag](https://image.prntscr.com/image/TdQQADGrStirHklov_9glA.png)



4- Go to Controllers -> ManageController, update index ActionView by adding this code befor return view() that will be used for updated design in next steps
````csharp
            var currentUser = await UserManager.FindByIdAsync(userId);
            ViewBag._2FAuthEnabled = currentUser.TwoFactorGoogleEnabled ? "enabled" : "disabled";
            if (currentUser.TwoFactorGoogleEnabled == true)
            {
                ViewBag.BtnText = "Disable";
                ViewBag.BtnCSS = "btn-danger";
            }
            else
            {
                ViewBag.BtnText = "Enable";
                ViewBag.BtnCSS = "btn-primary";
            }
            ViewBag.ErrorMessage = "";
````

5- Add new Action View for POST Method which is for Enable or Disable Two Factor Auth using Google Authenticator
````csharp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Index(string _2FAuthEnabled)
        {

            var userId = User.Identity.GetUserId();
            var currentUser = await UserManager.FindByIdAsync(userId);
            var is_2FAuthEnabled = _2FAuthEnabled == "enabled" ? false : true;

            string UserUniqueKey = Guid.NewGuid().ToString(); ;


            ViewBag.TwoFactorEnabled = is_2FAuthEnabled;
            ViewBag._2FAuthEnabled = currentUser.TwoFactorGoogleEnabled ? "enabled" : "disabled";
            ViewBag.TwoFactorCode = currentUser.TwoFactorGoogleCode;
            if (is_2FAuthEnabled == true)
            {

                ViewBag.BtnText = "Disable";
                ViewBag.BtnCSS = "btn-danger";

                //2FA Setup
                TwoFactorAuthenticator tfa = new TwoFactorAuthenticator();
                
                var setupInfo = tfa.GenerateSetupCode("ASPMVC_2FAuth",
                    User.Identity.Name, UserUniqueKey, 300, 300);
                ViewBag.BarcodeImageUrl = setupInfo.QrCodeSetupImageUrl;
                ViewBag.SetupCode = setupInfo.ManualEntryKey;
                currentUser.TwoFactorGoogleCode = UserUniqueKey;
                await UserManager.UpdateAsync(currentUser);
            }

            var model = new IndexViewModel
            {
                HasPassword = HasPassword(),
                PhoneNumber = await UserManager.GetPhoneNumberAsync(userId),
                TwoFactor = await UserManager.GetTwoFactorEnabledAsync(userId),
                Logins = await UserManager.GetLoginsAsync(userId),
                BrowserRemembered = await AuthenticationManager.TwoFactorBrowserRememberedAsync(userId)
            };
            return View(model);
        }
````

6- Add another Action View for submit TwoFactorAuthenticator
````csharp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Verify2FA(string passcode)
        {

            var userId = User.Identity.GetUserId();
            var currentUser = await UserManager.FindByIdAsync(userId);
            var token = passcode;
            TwoFactorAuthenticator tfa = new TwoFactorAuthenticator();
            string UserUniqueKey = currentUser.TwoFactorGoogleCode;
            bool isValid = tfa.ValidateTwoFactorPIN(UserUniqueKey, token);
            if (isValid)
            {
                currentUser.TwoFactorGoogleEnabled = true;
                await UserManager.UpdateAsync(currentUser);
                return RedirectToAction("Index");
            }
            else
                ViewBag.StatusMessage = "Invalid Passcode";

            return RedirectToAction("Index");
        }
````

7- Go to index View to update it by adding Enable and Disable Feature (Views -> Manage -> index.cshtml)

````html
    <div>
        <h2>Two Factor Authentication using Google Authenticator</h2>

        <div>
            @using (Html.BeginForm("index", "Manage", new { }, FormMethod.Post, new { @class = "", role = "form" }))
            {
            @Html.AntiForgeryToken()
            <input type="hidden" name="_2FAuthEnabled" value="@ViewBag._2FAuthEnabled" />

            <button type="submit" class="btn @ViewBag.BtnCSS">@ViewBag.BtnText</button>
            }
        </div>
        <p class="text-danger">@ViewBag.ErrorMessage</p>
        @if (ViewBag.TwoFactorEnabled != null && ViewBag.TwoFactorEnabled)
        {
        <div>
            <img src="@ViewBag.BarcodeImageUrl" />
        </div>
        <div>
            Manual Setup Code : @ViewBag.SetupCode
        </div>
        <div>
            @using (Html.BeginForm("Verify2FA", "Manage", FormMethod.Post))
            {
            @Html.AntiForgeryToken()
            <input type="text" name="passcode" />
            <input type="submit" class="btn btn-success" />
            }
        </div>
        }
    </div>
````

8- Go to AccountController, Update (POST) Action View for Login, before sign user in, will check if user enable google 2F Auth
````csharp
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            //Check User
            var user = await SignInManager.UserManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(model);
            }
            //Check Password
            var validCredentials = await SignInManager.UserManager.CheckPasswordAsync(user, model.Password);
            if (!validCredentials)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(model);
            }
            //If has TwoFactorGoogle
            else if (user.TwoFactorGoogleEnabled == true)
            {
                Session["username"] = model.Email;
                Session["password"] = model.Password;
                Session["RememberMe"] = model.RememberMe;
                return RedirectToAction("Verify2FA", new { ReturnUrl = returnUrl });
            }

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, change to shouldLockout: true
            var result = await SignInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, shouldLockout: false);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid login attempt.");
                    return View(model);
            }
        }


````

9- Now add new Action View for Two Factor Authentication page in AccountController

````csharp
        [AllowAnonymous]
        public ActionResult Verify2FA(string returnUrl)
        {
            //check if user login
            if (String.IsNullOrEmpty(Session["username"] + "") || String.IsNullOrEmpty(Session["password"] + ""))
                return RedirectToAction("Login");
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }
````

10 - add new View Verify2FA
````html
@{
    ViewBag.Title = "Verify2FA";
}

<h2>Two Factor Authentication using Google Authenticator</h2>

<div>
    @using (Html.BeginForm("Verify2FA", "Account", FormMethod.Post))
    {
        @Html.AntiForgeryToken()

        @Html.ValidationSummary(true, "", new { @class = "text-danger" })
        <input type="text" name="passcode" />
        <input type="submit" class="btn btn-success" />
    }
</div>
````

11- add POST Action View for Verify2FA view

````csharp

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Verify2FA(string passcode, string returnUrl)
        {
            try
            {
                if (String.IsNullOrEmpty(Session["username"] + "") || String.IsNullOrEmpty(Session["password"] + ""))
                    return RedirectToAction("Login");

                var user = await SignInManager.UserManager.FindByEmailAsync(Session["username"] + "");

                
                TwoFactorAuthenticator tfa = new TwoFactorAuthenticator();
                string UserUniqueKey = user.TwoFactorGoogleCode;
                bool isValid = tfa.ValidateTwoFactorPIN(UserUniqueKey, passcode);
                if (isValid)
                {//
                    var result = await SignInManager.PasswordSignInAsync(Session["username"] + "",
                        Session["password"] + "", Convert.ToBoolean(Session["RememberMe"] + ""), shouldLockout: false);
                    switch (result)
                    {
                        case SignInStatus.Success:
                            return RedirectToLocal(returnUrl);
                        case SignInStatus.LockedOut:
                            return View("Lockout");
                        case SignInStatus.RequiresVerification:
                            return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = Convert.ToBoolean(Session["RememberMe"] + "") });
                        case SignInStatus.Failure:
                        default:
                            ModelState.AddModelError("", "Invalid login attempt.");
                            return View("Login");
                    }
                }
                else
                {
                    ViewBag.ReturnUrl = returnUrl;
                    ViewBag.Msg = "Invalid Code ..";
                    return View();
                }

            }
            catch (Exception er)
            {
                ModelState.AddModelError("", er.Message);
            }
            return View();
        }

````

12- Test and View

* Download and install Google authenticator from the [Play Store](https://play.google.com/store/apps/details?id=com.google.android.apps.authenticator2&hl=en) for Android and from the [App Store](https://itunes.apple.com/us/app/google-authenticator/id388497605?mt=8) for iOS
* Run Project
* Register new User

![alt tag](https://image.prntscr.com/image/FJphO6pDScmRmm3ByNUzaA.png)
- - - -
* go to Manage page 

![alt tag](https://image.prntscr.com/image/WO5xYYpMT-mCPh5AIfclAA.png)
- - - -
* Enable Two Factor Authentication using Google Authenticator

![alt tag](https://image.prntscr.com/image/mBrluwVzQqqAbI0s3YPuMg.png)
- - - -
* Scan Barcode using Google Authenticator on your mobile

![alt tag](https://image.prntscr.com/image/O0X7OVrNTqi3hcKXixHmnQ.png)
- - - -
![alt tag](https://image.prntscr.com/image/FLiwKvUYQSaZGQQc1MCwDw.png)
- - - -
* Type Passcode, then click submit

* To test 2Factor Auth, logout, then login again, it must require 2Factor Auth using Google Authenticator
        
       
        
        



