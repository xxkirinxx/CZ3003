using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement;

public class FirebaseManager : MonoBehaviour
{
    //[SerializeField] QuestionManager questionManager;
    //Firebase variables
    [Header("Firebase")]
    public DependencyStatus dependencyStatus;
    public FirebaseAuth auth;    
    public static FirebaseUser User;
    public DatabaseReference DBreference;

    //Login variables
    [Header("Login")]
    public TMP_InputField emailLoginField;
    public TMP_InputField passwordLoginField;
    public TMP_Text warningLoginText;
    public TMP_Text confirmLoginText;

    //Register variables
    [Header("Register")]
    public TMP_InputField usernameRegisterField;
    public TMP_InputField emailRegisterField;
    public TMP_InputField passwordRegisterField;
    public TMP_InputField passwordRegisterVerifyField;
    public TMP_Text warningRegisterText;

    //User Data variables
    [Header("UserData")]
    public TMP_InputField usernameField;
    public TMP_InputField xpField;
    public TMP_InputField killsField;
    public TMP_InputField masteryField;
    public GameObject scoreElement;
    public Transform scoreboardContent;

    void Awake()
    {
        //Check that all of the necessary dependencies for Firebase are present on the system
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                //If they are avalible Initialize Firebase
                InitializeFirebase();
            }
            else
            {
                Debug.LogError("Could not resolve all Firebase dependencies: " + dependencyStatus);
            }
        });
    }

    private void InitializeFirebase()
    {
        Debug.Log("Setting up Firebase Auth");
        //Set the authentication instance object
        auth = FirebaseAuth.DefaultInstance;
        DBreference = FirebaseDatabase.DefaultInstance.RootReference;
    }
    public void ClearLoginFeilds()
    {
        emailLoginField.text = "";
        passwordLoginField.text = "";
    }
    public void ClearRegisterFeilds()
    {
        usernameRegisterField.text = "";
        emailRegisterField.text = "";
        passwordRegisterField.text = "";
        passwordRegisterVerifyField.text = "";
    }

    //Function for the login button
    public void LoginButton()
    {
        //questionManager.Awake();
        //Call the login coroutine passing the email and password
        StartCoroutine(Login(emailLoginField.text, passwordLoginField.text));
    }
    //Function for the register button
    public void RegisterButton()
    {
        //Call the register coroutine passing the email, password, and username
        StartCoroutine(Register(emailRegisterField.text, passwordRegisterField.text, usernameRegisterField.text));
    }
    //Function for the sign out button
    public void SignOutButton()
    {
        auth.SignOut();
        UIManager.instance.LoginScreen();
        ClearRegisterFeilds();
        ClearLoginFeilds();
    }

    public void EnterGameButton() {
        ClearRegisterFeilds();
        ClearLoginFeilds();
        SceneManager.LoadScene("Character Selection");
    }
    //Function for the save button
    public void SaveDataButton()
    {   StartCoroutine(UpdateStars(1,1,0));
        StartCoroutine(UpdateUsernameAuth(usernameField.text));
        StartCoroutine(UpdateUsernameDatabase(usernameField.text));
        StartCoroutine(UpdateKills(int.Parse(killsField.text)));
    }
    //Function for the scoreboard button
    // public void ScoreboardButton()
    // {        
    //     StartCoroutine(LoadScoreboardData());
    // }

    private IEnumerator Login(string _email, string _password)
    {
        //Call the Firebase auth signin function passing the email and password
        var LoginTask = auth.SignInWithEmailAndPasswordAsync(_email, _password);
        //Wait until the task completes
        yield return new WaitUntil(predicate: () => LoginTask.IsCompleted);

        if (LoginTask.Exception != null)
        {
            //If there are errors handle them
            Debug.LogWarning(message: $"Failed to register task with {LoginTask.Exception}");
            FirebaseException firebaseEx = LoginTask.Exception.GetBaseException() as FirebaseException;
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

            string message = "Login Failed!";
            switch (errorCode)
            {
                case AuthError.MissingEmail:
                    message = "Missing Email";
                    break;
                case AuthError.MissingPassword:
                    message = "Missing Password";
                    break;
                case AuthError.WrongPassword:
                    message = "Wrong Password";
                    break;
                case AuthError.InvalidEmail:
                    message = "Invalid Email";
                    break;
                case AuthError.UserNotFound:
                    message = "Account does not exist";
                    break;
            }
            warningLoginText.text = message;
        }
        else
        {
            //User is now logged in
            //Now get the result
            User = LoginTask.Result;
            Debug.LogFormat("User signed in successfully: {0} ({1})", User.DisplayName, User.Email);
            warningLoginText.text = "";
            confirmLoginText.text = "Logged In";
            // StartCoroutine(LoadUserData());

            yield return new WaitForSeconds(1);

            usernameField.text = User.DisplayName;
            //UIManager.instance.UserDataScreen(); // Change to user data UI
            confirmLoginText.text = "";
            ClearLoginFeilds();
            ClearRegisterFeilds();
            SceneManager.LoadScene("Character Selection");
        }
    }

    private IEnumerator Register(string _email, string _password, string _username)
    {
        if (_username == "")
        {
            //If the username field is blank show a warning
            warningRegisterText.text = "Missing Username";
        }
        else if(passwordRegisterField.text != passwordRegisterVerifyField.text)
        {
            //If the password does not match show a warning
            warningRegisterText.text = "Password Does Not Match!";
        }
        else 
        {
            //Call the Firebase auth signin function passing the email and password
            var RegisterTask = auth.CreateUserWithEmailAndPasswordAsync(_email, _password);
            //Wait until the task completes
            yield return new WaitUntil(predicate: () => RegisterTask.IsCompleted);

            if (RegisterTask.Exception != null)
            {
                //If there are errors handle them
                Debug.LogWarning(message: $"Failed to register task with {RegisterTask.Exception}");
                FirebaseException firebaseEx = RegisterTask.Exception.GetBaseException() as FirebaseException;
                AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

                string message = "Register Failed!";
                switch (errorCode)
                {
                    case AuthError.MissingEmail:
                        message = "Missing Email";
                        break;
                    case AuthError.MissingPassword:
                        message = "Missing Password";
                        break;
                    case AuthError.WeakPassword:
                        message = "Weak Password";
                        break;
                    case AuthError.EmailAlreadyInUse:
                        message = "Email Already In Use";
                        break;
                }
                warningRegisterText.text = message;
            }
            else
            {
                //User has now been created
                //Now get the result
                User = RegisterTask.Result;

                if (User != null)
                {
                    //Create a user profile and set the username
                    UserProfile profile = new UserProfile{DisplayName = _username};

                    //Call the Firebase auth update user profile function passing the profile with the username
                    var ProfileTask = User.UpdateUserProfileAsync(profile);
                    //Wait until the task completes
                    yield return new WaitUntil(predicate: () => ProfileTask.IsCompleted);
                    // StartCoroutine(UpdateInnerStars(0));
                    InitialisePlayerProfile();
                    StartCoroutine(UpdateUsernameDatabase(_username));
                    if (ProfileTask.Exception != null)
                    {
                        //If there are errors handle them
                        Debug.LogWarning(message: $"Failed to register task with {ProfileTask.Exception}");
                        warningRegisterText.text = "Username Set Failed!";
                    }
                    else
                    {
                        //Username is now set
                        //Now return to login screen
                        UIManager.instance.LoginScreen();                        
                        warningRegisterText.text = "";
                        ClearRegisterFeilds();
                        ClearLoginFeilds();
                    }
                }
            }
        }
    }

    private IEnumerator UpdateStars(int starsworld, int starssection, int input)
    {   
        string starworld = starsworld.ToString();
        string starsection = starssection.ToString();
        var DBTask = DBreference.Child("users").Child(User.UserId).Child("stars").Child(starworld).Child(starsection).SetValueAsync(input);
        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else
        {
            //Kills are now updated
        }
    }

    public void UniversalUpdateStars(int starsworld, int starssection, int input){
        StartCoroutine(UpdateStars(starsworld,starssection,input));
    }

    private IEnumerator UpdateBattleStats(int starsworld,int section, int input)
    {   
        string starsection = section.ToString();
        string starworld = starsworld.ToString();
        var DBTask = DBreference.Child("users").Child(User.UserId).Child("BattleStats").Child(starworld).Child(starsection).Child("Points").SetValueAsync(input);
        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else
        {
            //Kills are now updated
        }
    }
    private void InitialisePlayerProfile()
    {   
        StartCoroutine(UpdateBattleStats(1,1,0));
        StartCoroutine(UpdateBattleStats(1,2,0));
        StartCoroutine(UpdateBattleStats(1,3,0));
        StartCoroutine(UpdateBattleStats(2,1,0));
        StartCoroutine(UpdateBattleStats(2,2,0));
        StartCoroutine(UpdateBattleStats(2,3,0));
        StartCoroutine(UpdateBattleStats(3,1,0));
        StartCoroutine(UpdateBattleStats(3,2,0));
        StartCoroutine(UpdateBattleStats(3,3,0));
        StartCoroutine(UpdateStars(1,1,0));
        StartCoroutine(UpdateStars(1,2,0));
        StartCoroutine(UpdateStars(1,3,0));
        StartCoroutine(UpdateStars(2,1,0));
        StartCoroutine(UpdateStars(2,2,0));
        StartCoroutine(UpdateStars(2,3,0));
        StartCoroutine(UpdateStars(3,1,0));
        StartCoroutine(UpdateStars(3,2,0));
        StartCoroutine(UpdateStars(3,3,0));
    }
    private IEnumerator UpdateUsernameAuth(string _username)
    {
        //Create a user profile and set the username
        UserProfile profile = new UserProfile { DisplayName = _username };

        //Call the Firebase auth update user profile function passing the profile with the username
        var ProfileTask = User.UpdateUserProfileAsync(profile);
        //Wait until the task completes
        yield return new WaitUntil(predicate: () => ProfileTask.IsCompleted);

        if (ProfileTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {ProfileTask.Exception}");
        }
        else
        {
            //Auth username is now updated
        }        
    }

    private IEnumerator UpdateUsernameDatabase(string _username)
    {
        //Set the currently logged in user username in the database
        var DBTask = DBreference.Child("users").Child(User.UserId).Child("username").SetValueAsync(_username);

        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else
        {
            //Database username is now updated
        }
    }
    



    private IEnumerator UpdateKills(int _kills)
    {
        //Set the currently logged in user kills
        var DBTask = DBreference.Child("users").Child(User.UserId).Child("kills").SetValueAsync(_kills);

        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else
        {
            // Kills are now updated
        }
    }


    // private IEnumerator LoadUserData()
    // {
    //     //Get the currently logged in user data
    //     var DBTask = DBreference.Child("users").Child(User.UserId).GetValueAsync();

    //     yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

    //     if (DBTask.Exception != null)
    //     {
    //         Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
    //     }
    //     else if (DBTask.Result.Value == null)
    //     {
    //         //No data exists yet
    //         xpField.text = "0";
    //         killsField.text = "0";
    //         masteryField.text = "0";
    //     }
    //     else
    //     {
    //         //Data has been retrieved
    //         DataSnapshot snapshot = DBTask.Result;

    //         killsField.text = snapshot.Child("kills").Value.ToString();
    //     }
    // }

    // private IEnumerator LoadScoreboardData()
    // {
    //     //Get all the users data ordered by kills amount
    //     var DBTask = DBreference.Child("users").OrderByChild("mastery").GetValueAsync();

    //     yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

    //     if (DBTask.Exception != null)
    //     {
    //         Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
    //     }
    //     else
    //     {
    //         //Data has been retrieved
    //         DataSnapshot snapshot = DBTask.Result;

    //         //Destroy any existing scoreboard elements
    //         foreach (Transform child in scoreboardContent.transform)
    //         {
    //             Destroy(child.gameObject);
    //         }

    //         //Loop through every users UID
    //         foreach (DataSnapshot childSnapshot in snapshot.Children.Reverse<DataSnapshot>())
    //         {
    //             string username = childSnapshot.Child("username").Value.ToString();
    //             int kills = int.Parse(childSnapshot.Child("kills").Value.ToString());
    //             int mastery = int.Parse(childSnapshot.Child("mastery").Value.ToString());
    //             int xp = int.Parse(childSnapshot.Child("xp").Value.ToString());

    //             //Instantiate new scoreboard elements
    //             GameObject scoreboardElement = Instantiate(scoreElement, scoreboardContent);
    //             scoreboardElement.GetComponent<ScoreElement>().NewScoreElement(username, kills, mastery, xp);
    //         }

    //         //Go to scoareboard screen
    //         UIManager.instance.ScoreboardScreen();
    //     }
    // }
}