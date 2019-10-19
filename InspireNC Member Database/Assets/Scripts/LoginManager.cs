using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;
using Firebase.Database;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Net.Mail;

public class LoginManager : MonoBehaviour
{
    FirebaseManager firebaseManager;

    [SerializeField] 
    private TMP_InputField emailInput, passwordInput;

    [SerializeField]
    private TextMeshProUGUI memberName, classification, daysAttended, totalHours, averageAttendance, email, phoneNumber;

    [SerializeField]
    private GameObject messageScreen, mainScreen, programTextPrefab;

    [SerializeField]
    private GameObject unverfiedPage, editProfilePage, memberInfoPage, loginPage;

    [SerializeField]
    private TMP_InputField editProfileNumber, grade, school, address, parent1name, parent1email, parent1number, parent2name, parent2email, parent2number, editProfileEmail, age, gender;

    [SerializeField]
    private GameObject programTogglesContent;

    [SerializeField]
    private GameObject programTogglePrefab;

    [SerializeField]
    private GameObject programsContent;

    private PublicUserData publicData = null;
    private PrivateUserData privateData = null;
    private Attendance attendanceData = null;



    void Awake()
    {
        firebaseManager = GameObject.FindGameObjectWithTag("FirebaseManager").GetComponent<FirebaseManager>();
    }

    public void NextButton()
    {
        if (checkIfFilled() == false)
        {
            return;
        }
        else if (checkIfFilled() == true)
        {
            StartCoroutine(login(true));
        }
    }

    public void EditProfileButton()
    {
        if (checkIfFilled() == false)
        {
            return;
        }
        else if (checkIfFilled() == true)
        {
            StartCoroutine(EditProfile());
        }
    }

    public void HomeButton()
    {
        reset();
    }    

    public void ResendEmailButton()
    {
        StartCoroutine(resendEmail());
    }
   
   public void SaveProfileButton()
    {
        if (validatePhoneNumber() && validateEmail() && validateGender() )
            StartCoroutine(SaveProfile());
    }

    public void Update()
    {
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
    
    IEnumerator resendEmail()
    {
        firebaseManager.enableLoadingScreen();
        
        bool done = false;

        firebaseManager.auth.CurrentUser.SendEmailVerificationAsync().ContinueWith(task => 
        {
            if (task.IsCompleted)
            {
                done = true;
            }
        });

        yield return new WaitUntil(() => done == true);

        reset();
    }

    IEnumerator login(bool isItLoginPage)
    {
        bool done = false;
        bool ableToSignIn = true;

        string errorMessage = "";

        firebaseManager.enableLoadingScreen();
        firebaseManager.setErrorMessage("");
        firebaseManager.auth.SignOut();

        firebaseManager.auth.SignInWithEmailAndPasswordAsync(emailInput.text, passwordInput.text).ContinueWith(task => 
        {
            if (task.IsFaulted)
            {
                errorMessage = (task.Exception.ToString().Substring(task.Exception.ToString().IndexOf("FirebaseException: ") + 19, task.Exception.ToString().Substring(task.Exception.ToString().IndexOf("FirebaseException: ") + 19).IndexOf(".") + 1));
                ableToSignIn = false;
                done = true;
                return;
            }

            if (task.IsCompleted)

            done = true;
        });

        yield return new WaitUntil(() => done == true);
        done = false;

        firebaseManager.setErrorMessage(errorMessage);

        if (ableToSignIn == false)
        {
            firebaseManager.disableLoadingScreen();
            yield break;
        }

        loginPage.SetActive(false);

        if (firebaseManager.auth.CurrentUser.IsEmailVerified == false)
        {
            unverfiedPage.SetActive(true);
            firebaseManager.disableLoadingScreen();
            yield break;
        }
        
        memberInfoPage.SetActive(true);
        
        if (isItLoginPage == true)
        {
            messageScreen.SetActive(true);
            messageScreen.GetComponentInChildren<TextMeshProUGUI>().text = "Welcome " + firebaseManager.auth.CurrentUser.DisplayName + "!";
            yield return new WaitForSeconds(1);
            messageScreen.SetActive(false);
        }

        publicData = null;
        firebaseManager.getFirebaseReference("Users").Child("Public Data").Child(firebaseManager.auth.CurrentUser.DisplayName).Child(firebaseManager.auth.CurrentUser.UserId).Child("profile").GetValueAsync().ContinueWith(task => 
        {
            if (task.IsCanceled)
            {
                return;
            }
            if (task.IsFaulted)
            {
                print(task.Exception);
                return;
            }

            if (task.IsCompleted)
            {
                publicData = JsonUtility.FromJson<PublicUserData>(task.Result.GetRawJsonValue());
            }
        });

        yield return new WaitUntil(() => publicData != null);
        done = false;

        firebaseManager.getFirebaseReference("Users").Child("Private Data").Child(firebaseManager.auth.CurrentUser.DisplayName).Child(firebaseManager.auth.CurrentUser.UserId).Child("profile").GetValueAsync().ContinueWith(task => 
        {
            if (task.IsCanceled)
            {
                return;
            }
            if (task.IsFaulted)
            {
                return;
            }

            if (task.IsCompleted)
            {
                privateData = JsonUtility.FromJson<PrivateUserData>(task.Result.GetRawJsonValue());
            }
        });

        yield return new WaitUntil(() => privateData != null);
        done = false;

        firebaseManager.getFirebaseReference("Users").Child("Public Data").Child(firebaseManager.auth.CurrentUser.DisplayName).Child(firebaseManager.auth.CurrentUser.UserId).Child("attendance").GetValueAsync().ContinueWith(task => 
        {
            if (task.IsCanceled)
            {
                return;
            }
            if (task.IsFaulted)
            {
                return;
            }

            if (task.IsCompleted)
            {
                attendanceData = JsonUtility.FromJson<Attendance>(task.Result.GetRawJsonValue());
            }
        });

        yield return new WaitUntil(() => attendanceData != null);
        done = false;

        GameObject recyclableGameObject = null;
        foreach(Transform program in programsContent.transform)
        {
            Destroy(program.gameObject);
        }
        foreach (string program in publicData.programs)
        {
            recyclableGameObject = Instantiate(programTextPrefab, programsContent.transform, false);
            recyclableGameObject.GetComponent<TextMeshProUGUI>().text = program;
        }
        recyclableGameObject = null;

        email.text = "Email: " + publicData.inspirencEmail + ", " + privateData.personalEmail;
        phoneNumber.text = "Phone Number: (" + privateData.phoneNumber.Substring(0, 3) + ") " + privateData.phoneNumber.Substring(3, 3) + "-" + privateData.phoneNumber.Substring(6, 4 );
        memberName.text = publicData.name;
        classification.text = publicData.classification;
        daysAttended.text = "Days Attended: " + attendanceData.daysAttended.ToString();
        totalHours.text = "Total Hours: " + attendanceData.totalHours.ToString();
        averageAttendance.text = "Average Hours: " + attendanceData.averageHours.ToString();

        firebaseManager.disableLoadingScreen();

        yield break;
    }

    IEnumerator EditProfile()
    {
        DataSnapshot programs = null;

        firebaseManager.enableLoadingScreen();
        memberInfoPage.SetActive(false);
        editProfilePage.SetActive(true);

        editProfileNumber.text = privateData.phoneNumber;
        grade.text = publicData.grade.ToString();
        school.text = publicData.school;
        address.text = privateData.address;
        parent1name.text = privateData.parent1name;
        parent1email.text = privateData.parent1email;
        parent1number.text = privateData.parent1number;
        parent2name.text = privateData.parent2name;
        parent2email.text = privateData.parent2email;
        parent2number.text = privateData.parent2number;
        editProfileEmail.text = privateData.personalEmail;
        age.text = privateData.age.ToString();
        if (privateData.gender == "M")
        {
            gender.text = "Male";
        }
        else if (privateData.gender == "F")
        {
            gender.text = "Female";
        }
        else if (privateData.gender == "O")
        {
            gender.text = "Other";
        }

        firebaseManager.getFirebaseReference("Programs").GetValueAsync().ContinueWith(task => 
        {

            if (task.IsCanceled)
            {
                return;
            }
            if (task.IsFaulted)
            {
                return;
            }
            
            programs = task.Result;
        });
        yield return new WaitUntil(() => programs != null);

        GameObject recyclableProgram = null;
        using (var sequenceEnum = programs.Children.GetEnumerator())
        {
            for(int i = 0; i < programs.ChildrenCount; i++)
            {
                while (sequenceEnum.MoveNext())
                {
                    try
                    {
                        recyclableProgram = Instantiate(programTogglePrefab, programTogglesContent.transform, false);
                        recyclableProgram.GetComponentInChildren<TextMeshProUGUI>().text = (string)sequenceEnum.Current.Value;
                        foreach (string userProgram in publicData.programs)
                        {
                            if ((string)sequenceEnum.Current.Value == userProgram)
                            {
                                recyclableProgram.GetComponent<Toggle>().isOn = true;
                            }
                        }

                    }
                    catch(System.Exception e)
                    {
                        Debug.Log(e.Message);
                    }
                }
            }
        }
        programs = null;

        firebaseManager.disableLoadingScreen();
    }

    IEnumerator SaveProfile()
    {
        StopCoroutine(login(false));
        firebaseManager.enableLoadingScreen();
        bool done = false;
        List<string> programs = new List<string>();

        privateData.phoneNumber = editProfileNumber.text;
        publicData.grade = Int32.Parse(grade.text);
        publicData.school = school.text;
        privateData.address = address.text;
        privateData.parent1name = parent1name.text;
        privateData.parent1email = parent1email.text;
        privateData.parent1number = parent1number.text;
        privateData.parent2name = parent2name.text;
        privateData.parent2email = parent2email.text;
        privateData.parent2number = parent2number.text;
        privateData.personalEmail = editProfileEmail.text;
        privateData.age = Int32.Parse(age.text);
        privateData.gender = gender.text.Substring(0,1).ToUpper();

        foreach (Toggle toggle in gameObject.GetComponentsInChildren<Toggle>())
        {
            if (toggle.isOn)
                programs.Add(toggle.gameObject.GetComponentInChildren<TextMeshProUGUI>().text);
        }
        publicData.programs = programs;


        firebaseManager.reference.Child("Users").Child("Public Data").Child(publicData.name).Child(firebaseManager.auth.CurrentUser.UserId).Child("profile").SetRawJsonValueAsync(JsonUtility.ToJson(publicData)).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                return;
            }
            if (task.IsFaulted)
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

        firebaseManager.reference.Child("Users").Child("Private Data").Child(publicData.name).Child(firebaseManager.auth.CurrentUser.UserId).Child("profile").SetRawJsonValueAsync(JsonUtility.ToJson(privateData)).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                return;
            }
            if (task.IsFaulted)
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

        messageScreen.GetComponentInChildren<TextMeshProUGUI>().text = "Profile Updated Successfully!";
        messageScreen.SetActive(true);
        yield return new WaitForSeconds(1);
        messageScreen.SetActive(false);

        foreach (TMP_InputField input in editProfilePage.gameObject.GetComponentsInChildren<TMP_InputField>())
        {
            input.text = "";
        }
        foreach (Transform toggle in programTogglesContent.transform)
        {
            Destroy(toggle.gameObject);
        }
        
        editProfilePage.SetActive(false);
        StartCoroutine(login(false));

        yield break;
    }
    
    bool checkIfFilled()
    {
        bool filled = true;

        foreach(TMP_InputField input in gameObject.GetComponentsInChildren<TMP_InputField>())
        {
            if (string.IsNullOrEmpty(input.text))
            {
                filled = false;
            }
        }

        if (filled == true)
        {
            firebaseManager.setErrorMessage("");
            return true;
        }
        else
        {
            firebaseManager.setErrorMessage("All fields are required");
            return false;
        }
    }

    void reset()
    {
        StopAllCoroutines();
        firebaseManager.setErrorMessage("");
        loginPage.SetActive(true);
        foreach (TMP_InputField input in loginPage.gameObject.GetComponentsInChildren<TMP_InputField>())
        {
            input.text = "";
        }
        loginPage.SetActive(false);
        memberInfoPage.SetActive(true);
        email.text = "Email: ";
        phoneNumber.text = "(000) 000-0000";
        memberName.text = "Name";
        classification.text = "Member";
        daysAttended.text = "Days Attended: ";
        totalHours.text = "Total Hours: ";
        averageAttendance.text = "Average Attendance: ";
        foreach(Transform program in programsContent.transform)
        {
            Destroy(program.gameObject);
        }
        memberInfoPage.SetActive(false);
        messageScreen.SetActive(true);
        messageScreen.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = "MESSAGE";
        messageScreen.SetActive(false);
        loginPage.SetActive(true);
        editProfilePage.SetActive(false);
        unverfiedPage.SetActive(false);
        memberInfoPage.SetActive(false);
        gameObject.SetActive(false);
        firebaseManager.auth.SignOut();
        firebaseManager.disableLoadingScreen();
        mainScreen.SetActive(true);
        mainScreen.GetComponent<MainScreenManager>().Start();
        publicData = null;
        privateData = null;
        attendanceData = null;
        
    }

    bool validateEmail()
    {
        bool validated = true;

        string email = null;
        foreach (GameObject emailObject in GameObject.FindGameObjectsWithTag("Email"))
        {
            email = emailObject.GetComponentInChildren<TMP_InputField>().text;

            try 
            {
                string emailAddress = new MailAddress(email).Address;
            } 
            catch(FormatException) 
            {
                firebaseManager.setErrorMessage("Invalid Email");
                validated = false;
            }
        }

        if  (validated == true)
        {
            firebaseManager.setErrorMessage("");
        }
        else if (validated == false)
        {
            firebaseManager.setErrorMessage("Invalid Email(s)");
        }

        return validated;
    }

    bool validatePhoneNumber()
    {
        bool validated = true;

        string number = null;
        foreach (GameObject numberObject in GameObject.FindGameObjectsWithTag("Phone Number"))
        {
            number = numberObject.GetComponentInChildren<TMP_InputField>().text;

            if (!(number.Length == 10))
            {
                firebaseManager.setErrorMessage("Invalid Phone Number");
                validated = false;
            }
        }

        if  (validated == true)
        {
            firebaseManager.setErrorMessage("");
        }
        else if (validated == false)
        {
            firebaseManager.setErrorMessage("Invalid Phone Number(s)");
        }

        return validated;
    }

    bool validateGender()
    {
        if ((gender.text.ToUpper() == "MALE" || gender.text.ToUpper() == "FEMALE" || gender.text.ToUpper() == "OTHER"))
        {
            firebaseManager.setErrorMessage("");
            return true;
        }
            

        firebaseManager.setErrorMessage("Invalid Gender");
        return false;
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
