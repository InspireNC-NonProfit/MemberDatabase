using TMPro;
using UnityEngine;
using System.Collections;
using Firebase;
using Firebase.Database;
using Firebase.Unity.Editor;


public class FirebaseManager : MonoBehaviour
{
    public DatabaseReference reference;
    public Firebase.Auth.FirebaseAuth auth;

    [SerializeField]
    private GameObject ErrorHeader;

    [SerializeField]
    private GameObject loadingScreen, messageScreen, mainScreen;

    [SerializeField]
    private GameObject programCheckbox;

    [SerializeField]
    private GameObject programsContent;

    private float errorTime = 0f;

    void Awake()
    {
        // Set up the Editor before calling into the realtime database.
        FirebaseApp.DefaultInstance.SetEditorDatabaseUrl("https://inspirenc-database.firebaseio.com/");

        // Get the root reference location of the database.
        reference = FirebaseDatabase.DefaultInstance.RootReference;

        // Initialize authentication server.
        auth = Firebase.Auth.FirebaseAuth.DefaultInstance;

        Screen.SetResolution(1920, 1080, true);

        StartCoroutine(SignInDefaultUser());

    }

    public void setErrorMessage(string errorMessage)
    {
        if (errorMessage == "")
        {
            ErrorHeader.SetActive(false);
            return;
        }
        ErrorHeader.SetActive(true);
        ErrorHeader.GetComponentInChildren<TextMeshProUGUI>().text = "ERROR: " + errorMessage;
    }

    public DatabaseReference getFirebaseReference(string path)
    {
        return FirebaseDatabase.DefaultInstance.GetReference(path);
    }

    public void disableLoadingScreen()
    {
        errorTime = 0f;
        loadingScreen.SetActive(false);
    }

    public void enableLoadingScreen()
    {
        errorTime = Time.time;
        loadingScreen.SetActive(true);
    }

    IEnumerator SignInDefaultUser()
    {
        bool done = false;
        bool error = false;

        enableLoadingScreen();
        mainScreen.SetActive(false);
        auth.SignInWithEmailAndPasswordAsync("inspirenc@inspirenc.us", "bUfFoONeRy").ContinueWith(task => 
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                error = true;
                done = true;
                return;
            }
            else if (task.IsCompleted)
            {
                done = true;
            }
        });

        yield return new WaitUntil(() => done == true);
        done = false;

        if (error == true)
        {
            messageScreen.SetActive(true);
            messageScreen.GetComponentInChildren<TextMeshProUGUI>().text = "Unable to contact server";
            yield return new WaitForSeconds(5);
            Application.Quit();
        }

        FirebaseDatabase.DefaultInstance.GetReference("BOOT").GetValueAsync().ContinueWith(task => 
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                error = true;
                done = true;
                return;
            }
            else if ((string)task.Result.Value == "bafoonery")
            {
                done = true;
            }
        });

        yield return new WaitUntil(() => done == true);
        done = false;

        if (error == true)
        {
            messageScreen.SetActive(true);
            messageScreen.GetComponentInChildren<TextMeshProUGUI>().text = "Unable to contact server";
            yield return new WaitForSeconds(5);
            Application.Quit();
        }

        mainScreen.SetActive(true);
        disableLoadingScreen();
        yield break;
    }

    public void Update()
    {
        if(Application.internetReachability == NetworkReachability.NotReachable)
        {
            messageScreen.SetActive(true);
            messageScreen.GetComponentInChildren<TextMeshProUGUI>().text = "No Internet Connection!";
        }
        else
        {
            messageScreen.SetActive(false);
            messageScreen.GetComponentInChildren<TextMeshProUGUI>().text = "Message";
        }
    }

}
