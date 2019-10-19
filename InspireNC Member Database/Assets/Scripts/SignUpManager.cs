using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Firebase.Database;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Net.Mail;
public class SignUpManager : MonoBehaviour
{
    private int screenID = 0;
    private List<string> emails = new List<string>();
    private FirebaseManager firebaseManager;

    [SerializeField]
    private TMP_InputField passwordInput, confirmPasswordInput;

    [SerializeField]
    private GameObject[] pages;
    
    [SerializeField]
    private GameObject programCheckbox;

    [SerializeField]
    private GameObject mainScreen;
    GameObject errorHeader;
    
    [SerializeField]
    private GameObject programsContent;

    private 
    GameObject currentPage;
    PublicUserData publicUserData = null;
    PrivateUserData privateUserData = null;

    [SerializeField]
    private ToggleGroup genders;
    
    [SerializeField]
    private GameObject homeButton;

    [SerializeField]
    private TMP_InputField inspirencEmail, school, memberName, phoneNumber, grade, age, personalEmail, address, parent1name, parent1email, parent1number, parent2name, parent2email, parent2number;
    List<string> programs = new List<string>();
/* ============================================= */
    bool firstTimeEntry = true;
    bool update = false;
    TMP_InputField[] inputFields;
/* ============================================= */

    public void back()
    {
        reset();
    }
    public void nextScreen()
    {
        if (checkInputs() == false) 
        {
            update = true;
            return;
        }

        if (screenID == 5)
        {
            homeButton.SetActive(false);
            AddProgramsToList();
            StartCoroutine(PushUserToFirebase());
        }
        else if (screenID == 6)
        {
            reset();
            return;
        }
        else if (screenID == 0)
        {
            
            if (CheckPassword("confirm") == false)
            {
                return;
            }

            if (validateInspirencEmail(inspirencEmail.text) == false)
            {
                return;
            }
        }
        else if (screenID == 1 || screenID == 3 || screenID == 4)
        {
            foreach (GameObject email in GameObject.FindGameObjectsWithTag("Email"))
            {
                if (validateEmail(email.GetComponent<TMP_InputField>().text) == false)
                {
                    return;
                }
            }

            foreach (GameObject phone in GameObject.FindGameObjectsWithTag("Phone Number"))
            {
                if (validatePhoneNumber(phone.GetComponent<TMP_InputField>().text) == false)
                {
                    return;
                }
            }
        }
        if (screenID == 4)
        {
            StartCoroutine(GetPrograms());
        }

        pages[screenID].SetActive(false);
        screenID ++;
        pages[screenID].SetActive(true);

    }

    public IEnumerator GetPrograms()
    {
        firebaseManager.enableLoadingScreen();

        DataSnapshot snapshot = null;
        GameObject recycledProgramPrefab = null;

        var t = Task.Run(() => firebaseManager.getFirebaseReference("Programs").GetValueAsync().ContinueWith(task => 
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                return;
            }
            snapshot = task.Result;
        }));

        yield return new WaitUntil(() => snapshot != null);

        using (var sequenceEnum = snapshot.Children.GetEnumerator())
        {
            for(int i = 0; i < snapshot.ChildrenCount; i++)
            {
                while (sequenceEnum.MoveNext())
                {
                    try
                    {
                        recycledProgramPrefab =  Instantiate(programCheckbox, programsContent.transform, false);
                        recycledProgramPrefab.GetComponentInChildren<TextMeshProUGUI>().text = (string)sequenceEnum.Current.Value;

                    }
                    catch(System.Exception e)
                    {
                        Debug.Log(e.Message);
                    }
                }
            }
        }
        
        firebaseManager.disableLoadingScreen();
    }

    public IEnumerator PushUserToFirebase()
    {
        firebaseManager.enableLoadingScreen();
        Firebase.Auth.FirebaseUser newUser = null;
        bool done = false;
        string errorMessage = "";

        publicUserData = new PublicUserData(programs, inspirencEmail.text, school.text, memberName.text, "Member", Int32.Parse(grade.text));
        privateUserData = new PrivateUserData(personalEmail.text, address.text, parent1name.text, parent1email.text, parent1number.text, parent2name.text, parent2email.text, parent2number.text, phoneNumber.text, getCheckedGender(), Int32.Parse(age.text));

        firebaseManager.auth.CreateUserWithEmailAndPasswordAsync(publicUserData.inspirencEmail, passwordInput.text.ToString()).ContinueWith(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                errorMessage = errorMessage = (task.Exception.ToString().Substring(task.Exception.ToString().IndexOf("FirebaseException: ") + 19, task.Exception.ToString().Substring(task.Exception.ToString().IndexOf("FirebaseException: ") + 19).IndexOf(".") + 1));;
                done = true;
                return;
            }
            else if (task.IsCompleted)
            {
                newUser = task.Result;
                done = true;
            }
        });

        yield return new WaitUntil(() => done == true);
        done = false;

        if (errorMessage.Equals("The email address is already in use by another account."))
        {
            firebaseManager.setErrorMessage("The email address is already in use by another account.");
            yield return new WaitForSeconds(4);
            reset();
            yield break;
        }

        Firebase.Auth.FirebaseUser user = firebaseManager.auth.CurrentUser;
        if (user != null) {
            Firebase.Auth.UserProfile profile = new Firebase.Auth.UserProfile {
                DisplayName = publicUserData.name
            };
            user.UpdateUserProfileAsync(profile).ContinueWith(task => {
                if (task.IsCanceled || task.IsFaulted)
                {
                    return;
                }
                done = true;
            });
        }

        yield return new WaitUntil(() => done == true);
        done = false;

        Firebase.Auth.FirebaseUser currentUser = firebaseManager.auth.CurrentUser;

        currentUser.SendEmailVerificationAsync().ContinueWith(task =>
        {
            done = true;
        });

        yield return new WaitUntil(() => done == true);
        done = false;

        firebaseManager.reference.Child("Users").Child("Public Data").Child(publicUserData.name).Child(currentUser.UserId).Child("profile").SetRawJsonValueAsync(JsonUtility.ToJson(publicUserData)).ContinueWith(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                return;
            }

            if (task.IsCompleted)
            {
                done = true;
            }
        });

        yield return new WaitUntil(() => done == true);
        done = false;

        firebaseManager.reference.Child("Users").Child("Private Data").Child(publicUserData.name).Child(currentUser.UserId).Child("profile").SetRawJsonValueAsync(JsonUtility.ToJson(privateUserData)).ContinueWith(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                return;
            }

            if (task.IsCompleted)
            {
                done = true;
            }
        });

        yield return new WaitUntil(() => done == true);
        done = false;

        firebaseManager.reference.Child("Users").Child("Public Data").Child(publicUserData.name).Child(currentUser.UserId).Child("attendance").SetRawJsonValueAsync(JsonUtility.ToJson(new Attendance())).ContinueWith(task =>
        {
           if (task.IsCanceled || task.IsFaulted)
            {
                return;
            }

            if (task.IsCompleted)
            {
                done = true;
            }
        });

        yield return new WaitUntil(() => done == true);
        done = false;

        firebaseManager.reference.Child("Email List").Child(inspirencEmail.text.Substring(0, inspirencEmail.text.IndexOf("@"))).SetValueAsync(inspirencEmail.text).ContinueWith(task =>
        {
           if (task.IsCanceled || task.IsFaulted)
            {
                return;
            }

            if (task.IsCompleted)
            {
                done = true;
            }
        });

        yield return new WaitUntil(() => done == true);
        done = false;

        firebaseManager.disableLoadingScreen();
    }

    void reset()
    {
        firebaseManager.setErrorMessage("");
        StopAllCoroutines();
        screenID = 0;
        firstTimeEntry = true;
        foreach (GameObject page in pages)
        {
            page.SetActive(true);
            foreach (TMP_InputField input in page.GetComponentsInChildren<TMP_InputField>())
            {
                input.text = "";
            }
            page.SetActive(false);
        }
        pages[0].SetActive(true);
        update = false;
        firstTimeEntry = true;
        inputFields = null;
        currentPage = null;
        publicUserData = null;
        privateUserData = null;
        programs = new List<string>();
        pages[5].SetActive(true);
        foreach (Transform child in programsContent.transform)
        {
            Destroy(child.gameObject);
        }
        pages[5].SetActive(false);
        firebaseManager.disableLoadingScreen();
        firebaseManager.auth.SignOut();
        gameObject.SetActive(false);
        mainScreen.SetActive(true);
        mainScreen.GetComponent<MainScreenManager>().Start();
        homeButton.SetActive(true);
    }

    public void Awake()
    {
        firebaseManager = GameObject.FindGameObjectWithTag("FirebaseManager").GetComponent<FirebaseManager>();
    }

    public void Start()
    {
        StartCoroutine(getEmailListing());
    }

    public IEnumerator getEmailListing()
    {
        firebaseManager.enableLoadingScreen();

        bool done = false, error = false;
        firebaseManager.getFirebaseReference("Email List").GetValueAsync().ContinueWith(task => 
        {
            if (task.IsCompleted && !task.IsCanceled && !task.IsFaulted)
            {
                DataSnapshot listing = task.Result;
                if (listing.HasChildren)
                {
                    foreach (DataSnapshot snapshot in listing.Children)
                    {
                        emails.Add((string)snapshot.Value);
                    }
                }
            }
            else
            {
                error = true;
            }

            done = true;
        });

        yield return new WaitUntil(() => done == true);

        if (error == true)
        {
            firebaseManager.setErrorMessage("Unable to reach servers!");
            yield return new WaitForSeconds(3);
            Application.Quit();
        }

        firebaseManager.disableLoadingScreen();
    }

    public void Update()
    {
        if (update == true)
        {
            update = !checkInputs();
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            {
                if (EventSystem.current.currentSelectedGameObject != null)
                {
                    Selectable selectable = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnDown();
                    if (selectable != null)
                        selectable.Select();
                }
            }
        }
    }

    bool CheckPassword(string confirm)
    {
        if (passwordInput.text.Length < 8)
        {
            if (confirm == "confirm" && firstTimeEntry)
            {
                firebaseManager.setErrorMessage("Password must have a minimum of 8 characters");
                firstTimeEntry = false;
            }
            else if (firstTimeEntry == false)
            {
                firebaseManager.setErrorMessage("Password must have a minimum of 8 characters");
            }

            return false;
        }
        else if ((passwordInput.text != confirmPasswordInput.text))
        {
            if (confirm == "confirm" && firstTimeEntry)
            {
                firebaseManager.setErrorMessage("Passwords do not match");
                firstTimeEntry = false;
            }
            else if (firstTimeEntry == false)
            {
                firebaseManager.setErrorMessage("Passwords do not match");
            }

            return false;
        }

        else
        {
            firebaseManager.setErrorMessage("");
            return true;
        }
    }

    public void CheckPasswords(string confirm)
    {
        CheckPassword(confirm);
    }
    
    public bool checkInputs()
    {
        currentPage = pages[screenID];

        inputFields = pages[screenID].GetComponentsInChildren<TMP_InputField>();

        foreach (TMP_InputField field in inputFields)
        {
            if (string.IsNullOrEmpty(field.text))
            {
                firebaseManager.setErrorMessage("All fields are required");
                return false;
            }
        }

        firebaseManager.setErrorMessage("");
        return true;
    }

    bool validateInspirencEmail(string email)
    {
        if (emails.Count != 0)
        {
            foreach (string emailString in emails)
            {
                if (email == emailString)
                {
                    firebaseManager.setErrorMessage("Email already exists!");
                    inspirencEmail.text = "";
                    return false;
                }
            }
        }
    
        if (email.ToLower().Contains("@inspirenc.us") && email.IndexOf('@') > 0 && email.Split('@')[1] == "inspirenc.us")
        {
            firebaseManager.setErrorMessage("");
            return true;
        }
        else
        {
            firebaseManager.setErrorMessage("Invalid InspireNC Email");
            return false;
        }
    }

    bool validateEmail(string email)
    {
        try 
        {
            string emailAddress = new MailAddress(email).Address;
        } 
        catch(FormatException) 
        {
            firebaseManager.setErrorMessage("Invalid Email");
            return false;
        }

        firebaseManager.setErrorMessage("");
        return true;
    }

    bool validatePhoneNumber(string phoneNumber)
    {
        if (phoneNumber.Length != 10)
        {
            firebaseManager.setErrorMessage("Invalid phone number");
            return false;
        }
        else
        {
            firebaseManager.setErrorMessage("");
            return true;
        }
    }

    public void validateEmail()
    {
        foreach (GameObject email in GameObject.FindGameObjectsWithTag("Email"))
        {
            validateEmail(email.GetComponent<TMP_InputField>().text);
        }
        
    }

    public void validateInspirencEmail()
    {
        foreach (GameObject email in GameObject.FindGameObjectsWithTag("Email"))
        {
            validateInspirencEmail(email.GetComponent<TMP_InputField>().text);
        }
    }

    public void validatePhoneNumber()
    {
        foreach (GameObject phone in GameObject.FindGameObjectsWithTag("Phone Number"))
        {
            validatePhoneNumber(phone.GetComponent<TMP_InputField>().text);
        }
    }

    void AddProgramsToList()
    {
        firebaseManager.enableLoadingScreen();
        foreach (Toggle programToggle in programsContent.GetComponentsInChildren<Toggle>())
        {
            if (programToggle.isOn)
            {
                programs.Add(programToggle.gameObject.GetComponentInChildren<TextMeshProUGUI>().text);
            }
        }
    }

    string  getCheckedGender()
    {
        string gender = null;
        foreach (Toggle toggle in genders.GetComponentsInChildren<Toggle>())
        {
            if (toggle.isOn)
            {
                gender = toggle.GetComponentInChildren<TextMeshProUGUI>().text.Substring(0,1);
            }
        }

        return gender;
    }

    public void ontext(TMP_InputField input)
    {
        string inputString = input.text;

        if (inputString.Length == 1 && inputString.Contains("\\"))
        {
            input.text = "";
        }
        else if (inputString.Length > 1 && input.caretPosition == inputString.Length-1 && inputString.Contains("\\"))
        {
            input.text = inputString.Substring(0, inputString.IndexOf("\\")) + inputString.Substring(inputString.IndexOf("\\") + 1);
        }
        else if (inputString.Contains("\\"))
        {
            inputString = inputString.Substring(0, inputString.IndexOf("\\")) + inputString.Substring(inputString.IndexOf("\\") + 1);
            input.text = inputString;
            input.caretPosition = input.caretPosition - 1; 
        }
    }
}
