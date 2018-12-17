using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;

public class SpotifyController : MonoBehaviour
{

    void Start()
    {
        // if (!PlayerPrefs.HasKey("access_token"))
        // {
        // }
        print("Refreshing token");
        StartCoroutine(GetAndSaveToken());
    }

    IEnumerator GetAndSaveToken()
    {
        string refresh_token = "AQB_BRrIRHy4kYgc42srVPz2hIJVLYo2TuOBswPK4foCx2VfZ1leH4zBZV4qJdanDQ_CfEe8wqzwp-ETFlEXzmcg-ANb5tclnO2NVyhGzMlv5dW03fSLdLYwRsiNnrSNhbGyMg";

        string encoded = "Basic ZjIyYzI5ZTQ3ODdlNGExODlmNWRhZTFmMDdkZjY2ZTM6ZGE3MmUwYWNjNzM2NDgwNGIxNTNjMDQyNTRmYjVhNDQ=";

        string uri = "https://accounts.spotify.com/api/token";

		WWWForm form = new WWWForm();
        form.AddField("grant_type", "refresh_token");
		form.AddField("refresh_token", refresh_token);

        using (UnityWebRequest www = UnityWebRequest.Post(uri, form))
        {
			www.SetRequestHeader("Authorization", encoded);

            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log("Error!");
                Debug.Log(www.error);
				Debug.Log(www.responseCode);
            }
            else
            {
                print("Token refreshed");
                Debug.Log(www.downloadHandler.text);
            }
        }

    }


    IEnumerator GetText()
    {
        using (UnityWebRequest www = UnityWebRequest.Get("https://www.google.com"))
        {
            /* Do the request */
            yield return www.Send();

            /* Handle error */
            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
            }
            else
            {
                // Show results as text
                Debug.Log(www.downloadHandler.text);
            }
        }
    }
}
