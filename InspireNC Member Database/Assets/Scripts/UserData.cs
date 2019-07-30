using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class PublicUserData
{
    /* ======================= Public User Data ======================= */ 
    
    public List<string> programs;
    public string inspirencEmail, school, name, classification;
    public int grade;

    public PublicUserData(List<string> programs, string inspirencEmail, string school, string name, string classification, int grade)
    {
        this.programs = programs;
        this.inspirencEmail = inspirencEmail;
        this.school = school;
        this.name = name;
        this.classification = classification;
        this.grade = grade;
    }

    public PublicUserData() {}
}

[System.Serializable]
public class PrivateUserData
{
    /* ======================= Private User Data ======================= */
    
    public string personalEmail, address;
    public string parent1name, parent1email, parent1number, parent2name, parent2email, parent2number, phoneNumber; 

    public PrivateUserData(string personalEmail, string address, string parent1name, string parent1email, string parent1number, string parent2name, string parent2email, string parent2number, string phoneNumber)
    {
        this.personalEmail = personalEmail;
        this.address = address;
        this.parent1name = parent1name;
        this.parent1email = parent1email;
        this.parent1number = parent1number;
        this.parent2name = parent2name;
        this.parent2email = parent2email;
        this.parent2number = parent2number;
        this.phoneNumber = phoneNumber;
    }

    public PrivateUserData() {}
}

[System.Serializable]
public class Attendance
{
    public double totalHours = 0, averageHours = 0;
    public int daysAttended = 0;
    public bool signedIn = false;
    public long lastSignInTime = 0, lastSignOutTime = 0;

    public Attendance() {}

    public Attendance(double totalHours, double averageHours, int daysAttended, bool signedIn, long lastSignInTime, long lastSignOutTime)
    {
        this.totalHours = totalHours;
        this.averageHours = averageHours;
        this.daysAttended = daysAttended;
        this.signedIn = signedIn;
        this.lastSignInTime = lastSignInTime;
        this.lastSignOutTime = lastSignOutTime;
    }
}
