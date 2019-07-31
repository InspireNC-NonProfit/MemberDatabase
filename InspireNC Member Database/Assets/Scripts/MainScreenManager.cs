using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using System;
using TMPro;

public class MainScreenManager : MonoBehaviour
{
    private FirebaseManager firebaseManager;

    [SerializeField]
    private GameObject signUpScreen, loginScreen, memberInfoScreen, mainScreen, messageScreen;

    [SerializeField]
    private TMP_InputField searchBar;

    [SerializeField]
    private GameObject memberContentGameObject, memberPrefab;

    [SerializeField]
    private GameObject noMemberText;

    [SerializeField]
    private TextMeshProUGUI gradeText, schoolText, inspirencEmailText;

    [SerializeField]
    private GameObject programTextPrefab, programContentGameObject, signInOutButton;

    [SerializeField]
    private User memberInfoObject;

    private List<GameObject> userList = new List<GameObject>();
    
    public void Awake()
    {
        firebaseManager = GameObject.FindGameObjectWithTag("FirebaseManager").GetComponent<FirebaseManager>();
    }

    public void Start()
    {
        reset();
        StartCoroutine(PopulateMemberListing());
    }

    public void SearchUsers()
    {
        if (searchBar.text == "")
        {
            foreach (GameObject user in userList)
            {
                user.SetActive(true);
            }
        }
        else 
        {
            foreach (GameObject user in userList)
            {
                if (user.GetComponent<User>().memberName.Contains(searchBar.text))
                {
                    user.SetActive(true);
                }
                else
                {
                    user.SetActive(false);
                }
            }
        }
    }
    public void LoginButton()
    {
        loginScreen.SetActive(true);
        disableThisScreen();
    }

    public void SignUpButton()
    {
        signUpScreen.SetActive(true);
        disableThisScreen();
    }

    void disableThisScreen()
    {
        reset();
        gameObject.SetActive(false);
    }

    public void HomeButton()
    {
        reset();
        Start();
    }

    public void SignInOutButton()
    {
        if (signInOutButton.GetComponentInChildren<TextMeshProUGUI>().text == "Sign In")
        {
            StartCoroutine(SignIn());
        }
        else
        {
            StartCoroutine(SignOut());
        }
    }

    void reset()
    {
        StopAllCoroutines();
        searchBar.text = "";
        userList = new List<GameObject>();
        mainScreen.SetActive(true);
        memberInfoScreen.SetActive(true);
        noMemberText.SetActive(false);
        foreach (Transform member in memberContentGameObject.transform)
        {
            Destroy(member.gameObject);
        }
        memberInfoObject.SetName("");
        memberInfoObject.SetClassification("");
        memberInfoObject.memberName = null;
        memberInfoObject.userID = null;
        inspirencEmailText.text = "Email: ";
        gradeText.text = "Grade: ";
        schoolText.text = "School: ";
        messageScreen.SetActive(true);
        messageScreen.GetComponentInChildren<TextMeshProUGUI>().text = "";
        messageScreen.SetActive(false);
        foreach (Transform program in programContentGameObject.transform)
        {
            Destroy(program.gameObject);
        }
        memberInfoScreen.SetActive(false);
    }

    public void UserButton(string memberName, string userID)
    {
        StartCoroutine(openUser(memberName, userID));
    }

    IEnumerator PopulateMemberListing()
    {
        firebaseManager.enableLoadingScreen();
        bool done = false;

        if (firebaseManager.auth.CurrentUser == null)
        {
            firebaseManager.auth.SignInWithEmailAndPasswordAsync("inspirenc@inspirenc.us", "bUfFoONeRy").ContinueWith(task => 
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    return;
                }
                else if (task.IsCompleted)
                {
                    done = true;
                }
            });

            yield return new WaitUntil(() => done == true);
            done = false;
        }
        DataSnapshot users = null;

        firebaseManager.getFirebaseReference("Users/Public Data").GetValueAsync().ContinueWith(task => 
        {   
            if (task.IsCanceled || task.IsFaulted)
            {
                return;
            }
            else if (task.IsCompleted)
            {
                users = task.Result;
            }
        });

        yield return new WaitUntil(() => users != null);
        done = false;

        if (users.HasChildren == false)
        {
            noMemberText.SetActive(true);
            noMemberText.GetComponentInChildren<TextMeshProUGUI>().text = "No Registered Members";
            firebaseManager.disableLoadingScreen();
            yield break;
        }

        noMemberText.SetActive(false);

        GameObject recyclableGameObject = null;
        foreach (DataSnapshot memberName  in users.Children)
        {
            foreach (DataSnapshot userID in memberName.Children)
            {
                PublicUserData userData = JsonUtility.FromJson<PublicUserData>(userID.Child("profile").GetRawJsonValue());
                recyclableGameObject = Instantiate(memberPrefab, memberContentGameObject.transform, false);
                recyclableGameObject.GetComponent<User>().SetName(userData.name);
                recyclableGameObject.GetComponent<User>().SetClassification(userData.classification);
                recyclableGameObject.GetComponent<User>().memberName = userData.name;
                recyclableGameObject.GetComponent<User>().userID = userID.Key;
                userList.Add(recyclableGameObject);
            }
        }

        firebaseManager.disableLoadingScreen();
    }

    IEnumerator openUser(string memberName, string userID)
    {
        firebaseManager.enableLoadingScreen();
        PublicUserData userData = null;
        Attendance attendanceData = null;

        firebaseManager.getFirebaseReference("Users/Public Data").Child(memberName).Child(userID).Child("attendance").GetValueAsync().ContinueWith(task => 
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                print("retrieve data encountered an error: " + task.Exception);
                return;
            }

            if (task.IsCompleted)
            {
                attendanceData = JsonUtility.FromJson<Attendance>(task.Result.GetRawJsonValue());
            }
        });

        yield return new WaitUntil(() => attendanceData != null);

        firebaseManager.getFirebaseReference("Users/Public Data").Child(memberName).Child(userID).Child("profile").GetValueAsync().ContinueWith(task => 
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                print("retrieve data encountered an error: " + task.Exception);
                return;
            }

            if (task.IsCompleted)
            {
                userData = JsonUtility.FromJson<PublicUserData>(task.Result.GetRawJsonValue());
            }
        });

        yield return new WaitUntil(() => userData != null);

        mainScreen.SetActive(false);
        memberInfoScreen.SetActive(true);
        memberInfoObject.SetName(userData.name);
        memberInfoObject.SetClassification(userData.classification);
        memberInfoObject.memberName = userData.name;
        memberInfoObject.userID = userID;
        inspirencEmailText.text = "Email: " + userData.inspirencEmail;
        gradeText.text = "Grade: " + userData.grade.ToString();
        schoolText.text = "School: " +  userData.school;
        GameObject recyclableGameObject = null;
        foreach (string program in userData.programs)
        {
            recyclableGameObject = Instantiate(programTextPrefab, programContentGameObject.transform, false);
            recyclableGameObject.GetComponent<TextMeshProUGUI>().text = program;

        }
        if (attendanceData.signedIn == true)
        {
            signInOutButton.GetComponentInChildren<TextMeshProUGUI>().text = "Sign Out";
        }
        else
        {
            signInOutButton.GetComponentInChildren<TextMeshProUGUI>().text = "Sign In";
        }

        firebaseManager.disableLoadingScreen();
    }

    IEnumerator SignIn()
    {
        bool done = false;
        firebaseManager.enableLoadingScreen();

        firebaseManager.getFirebaseReference("Users/Public Data").Child(memberInfoObject.memberName).Child(memberInfoObject.userID).Child("attendance").Child("signedIn").SetValueAsync(true).ContinueWith(task => 
        {
            if (task.IsCanceled || task.IsCanceled)
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

        Dictionary<string, object> timestamp = new Dictionary<string, object>();
        timestamp[".sv"] = "timestamp";

        firebaseManager.reference.Child("Users").Child("Public Data").Child(memberInfoObject.memberName).Child(memberInfoObject.userID).Child("attendance").Child("lastSignInTime").SetValueAsync(timestamp).ContinueWith(task => 
        {
            if (task.IsCanceled || task.IsCanceled)
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

        messageScreen.SetActive(true);
        messageScreen.GetComponentInChildren<TextMeshProUGUI>().text = "Signed In!";
        yield return new WaitForSeconds(1);
        messageScreen.SetActive(false);

        reset();
        Start();
        yield break;
    }   

    IEnumerator SignOut()
    {
        bool done = false;
        firebaseManager.enableLoadingScreen();

        firebaseManager.getFirebaseReference("Users/Public Data").Child(memberInfoObject.memberName).Child(memberInfoObject.userID).Child("attendance").Child("signedIn").SetValueAsync(false).ContinueWith(task => 
        {
            if (task.IsCanceled || task.IsCanceled)
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

        Dictionary<string, object> timestamp = new Dictionary<string, object>();
        timestamp[".sv"] = "timestamp";

        firebaseManager.reference.Child("Users").Child("Public Data").Child(memberInfoObject.memberName).Child(memberInfoObject.userID).Child("attendance").Child("lastSignOutTime").SetValueAsync(timestamp).ContinueWith(task => 
        {
            if (task.IsCanceled || task.IsCanceled)
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

        Attendance attendanceData = null;

        firebaseManager.getFirebaseReference("Users/Public Data").Child(memberInfoObject.memberName).Child(memberInfoObject.userID).Child("attendance").GetValueAsync().ContinueWith(task => 
        {
            if (task.IsCanceled || task.IsCanceled)
            {
                return;
            }
            if (task.IsCompleted)
            {
                attendanceData = JsonUtility.FromJson<Attendance>(task.Result.GetRawJsonValue());
            }
        });

        yield return new WaitUntil(() => attendanceData != null);

        DateTime lastSignInTime = FromUnixTime(attendanceData.lastSignInTime);
        DateTime lastSignOutTime = FromUnixTime(attendanceData.lastSignOutTime);

        double durationInHours = (attendanceData.lastSignOutTime - attendanceData.lastSignInTime)/1000.0/60.0/60.0;

        print(durationInHours);

        if (durationInHours > 5 && lastSignInTime.Month > 4 && lastSignOutTime.Month > 4)
        {
            durationInHours = 5.0;
        }
        else if (durationInHours > 12 && lastSignInTime.Month <= 4 && lastSignOutTime.Month <= 4)
        {
            durationInHours = 12.0;
        }

        /* if (durationInHours > 0.5) */ attendanceData.daysAttended ++;
        attendanceData.totalHours += durationInHours;
        attendanceData.averageHours = attendanceData.totalHours/attendanceData.daysAttended;

        firebaseManager.reference.Child("Users/Public Data").Child(memberInfoObject.memberName).Child(memberInfoObject.userID).Child("attendance").SetRawJsonValueAsync(JsonUtility.ToJson(attendanceData)).ContinueWith(task => 
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

        messageScreen.SetActive(true);
        messageScreen.GetComponentInChildren<TextMeshProUGUI>().text = "Signed Out!";
        yield return new WaitForSeconds(1);
        messageScreen.SetActive(false);

        reset();
        Start();

        yield break;
    }

public DateTime FromUnixTime(long unixTime)
{
    TimeZoneInfo easternZone = TimeZoneInfo.Local;
    DateTime easternTime = TimeZoneInfo.ConvertTimeFromUtc(epoch.AddMilliseconds(unixTime), easternZone);
    return easternTime;
}
private readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}
