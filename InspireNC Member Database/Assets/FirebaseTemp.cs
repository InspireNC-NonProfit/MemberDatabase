using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Auth;
using Firebase.Unity.Editor;

public class FirebaseTemp : MonoBehaviour
{
    public DatabaseReference reference;
    public Firebase.Auth.FirebaseAuth auth;

    // Start is called before the first frame update
    void Awake()
    {
        // Set up the Editor before calling into the realtime database.
        FirebaseApp.DefaultInstance.SetEditorDatabaseUrl("https://inspirenc-database.firebaseio.com/");

        // Get the root reference location of the database.
        reference = FirebaseDatabase.DefaultInstance.RootReference;

        // Initialize authentication server.
        auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
        
    }

    public void Start()
    {
        StartCoroutine(SignInDefaultUser());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator SignInDefaultUser()
    {
        bool done = false;
        bool error = false;
        List<string> emails = new List<string>();
        
        auth.SignInWithEmailAndPasswordAsync("donotdelete@inspirenc.us", "TbRXffBJBGg3yqVaHFCdowZZG4Bv0MrobigcXaRrcIn3VxSVGq").ContinueWith(task => 
        {
            if (task.IsFaulted || task.IsCanceled)
            {
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

        FirebaseDatabase.DefaultInstance.GetReference("Email List").GetValueAsync().ContinueWith(task => 
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("error recieving email list: " + task.Exception);
                error = true;
                done = true;
                return;
            }
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.ChildrenCount < 1 || snapshot.HasChildren == false)
                {
                    Debug.LogError("no emails");
                    error = true;
                    done = true;
                    return;
                }

                foreach (DataSnapshot snapshotEmail in snapshot.Children)
                {
                    string email = (string)snapshotEmail.Value;
                    emails.Add(email);
                }

                done = true;
            }
        });

        yield return new WaitUntil(() => done == true);
        done = false;
        
        if (error)
        {
            yield break;
        }

        StartCoroutine(verifyAndReset(emails));
        
    }

    IEnumerator verifyAndReset(List<string> emails)
    {
        bool done = false;

        foreach (string email in emails)
        {
            FirebaseUser user = null;
            auth.SignInWithEmailAndPasswordAsync(email, "password").ContinueWith(task => 
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("error in sign in: " + task.Exception);
                    return;
                }
                if (task.IsCompleted)
                {
                    user = task.Result;
                    print(email + " signed in! " + user.UserId);
                    done = true;
                }
            });

            yield return new WaitUntil(() => done == true);
            done = false;

            auth.CurrentUser.SendEmailVerificationAsync().ContinueWith(task1 => 
            {
                if (task1.IsCanceled || task1.IsFaulted)
                {
                    Debug.LogError("error in sending verification email: " + task1.Exception + " " + user.Email);
                    return;
                }
                if (task1.IsCompleted)
                {
                    print("Verification email sent for " + user.Email);
                    done = true;
                }
            });

            yield return new WaitUntil(() => done == true);
            done = false;

            auth.SendPasswordResetEmailAsync(email).ContinueWith(task => 
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("error in sending password reset email: " + email);
                    return;
                }
                if (task.IsCompleted)
                {
                    print("password reset email sent for " + email);
                    done = true;
                }
            });

            yield return new WaitUntil(() => done == true);
            done = false;

            Debug.LogWarning("finished " + auth.CurrentUser.Email);

            auth.SignOut();
        }
    }

}
