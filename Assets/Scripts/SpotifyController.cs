using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;

public class SpotifyController : MonoBehaviour
{

    #region Private attributes
    private string refresh_token = "AQB_BRrIRHy4kYgc42srVPz2hIJVLYo2TuOBswPK4foCx2VfZ1leH4zBZV4qJdanDQ_CfEe8wqzwp-ETFlEXzmcg-ANb5tclnO2NVyhGzMlv5dW03fSLdLYwRsiNnrSNhbGyMg";
    private string encoded = "Basic ZjIyYzI5ZTQ3ODdlNGExODlmNWRhZTFmMDdkZjY2ZTM6ZGE3MmUwYWNjNzM2NDgwNGIxNTNjMDQyNTRmYjVhNDQ=";
    private string uri = "https://accounts.spotify.com/api/token";
    private bool refreshingToken = true;
    private bool requestingTrackInfo = false;
    #endregion

    #region Public attributes
    public float requestCooldown;
    #endregion

    void Update()
    {
        /* If we're not getting track info and it's not on cooldown period, get track info */
        if (!requestingTrackInfo) StartCoroutine(GetTrackInfo());
    }

    IEnumerator GetTrackInfo()
    {
        /* Set control variable to true */
        requestingTrackInfo = true;

        /* Refresh token if there's not one saved or if it has expired */
        if (!PlayerPrefs.HasKey("access_token") || TokenHasExpired())
        {
            Debug.Log("The access_token has expired, refreshing token...");
            StartCoroutine(RefreshToken());

            /* Wait for the refresh token request */
            while (refreshingToken) yield return new WaitForSeconds(0.1f);
        }

        /* Request track info */
        string url = "https://api.spotify.com/v1/me/player";
        string auth = "Bearer " + PlayerPrefs.GetString("access_token");

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            /* Set Authorization deader */
            www.SetRequestHeader("Authorization", auth);

            yield return www.Send();

            /* Handle error */
            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log("Error requesting track data.");
                Debug.Log(www.error);
                Debug.Log(www.responseCode);
            }
            else
            {
                Debug.Log(www.downloadHandler.text);

                /* Wait the cooldown period */
                yield return new WaitForSeconds(requestCooldown);

                /* Releasing control variable */
                requestingTrackInfo = false;
            }
        }
    }

    IEnumerator RefreshToken()
    {
        /* Setting the control variable as true */
        refreshingToken = true;

        /* Setting HTTP form boddy */
        WWWForm form = new WWWForm();
        form.AddField("grant_type", "refresh_token");
        form.AddField("refresh_token", refresh_token);

        using (UnityWebRequest www = UnityWebRequest.Post(uri, form))
        {
            /* Setting header */
            www.SetRequestHeader("Authorization", encoded);

            /* Sends request */
            yield return www.SendWebRequest();

            /* Handling errors */
            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log("Error refreshing token.");
                Debug.Log(www.error);
                Debug.Log(www.responseCode);
            }
            else
            {
                Debug.Log("The access_token has been refreshed successfully.");
                string response = www.downloadHandler.text;

                /* Saving the access_token and it's expiration_date */
                SetToken(response);

                /* Releasing control variable */
                refreshingToken = false;
            }
        }
    }

    void SetToken(string response)
    {
        /* Creating object from JSON response */
        SpotifyToken spotifyToken = SpotifyToken.CreateFromJSON(response);

        /* Saving access_token into device */
        PlayerPrefs.SetString("access_token", spotifyToken.access_token);

        /* Saving the expiration date */
        DateTime expDate = System.DateTime.Now.AddSeconds(spotifyToken.expires_in);
        string binaryDate = expDate.ToBinary().ToString();
        PlayerPrefs.SetString("expiration_date", binaryDate);
    }

    bool TokenHasExpired()
    {
        /* Getting the expiration_date and converting it to a comparable date format */
        long temp = Convert.ToInt64(PlayerPrefs.GetString("expiration_date"));
        DateTime expDate = DateTime.FromBinary(temp);
        DateTime current = System.DateTime.Now;

        /* Returns true if expDate is earlier than current */
        return DateTime.Compare(expDate, current) < 0;
    }
}

#region JSON classes
public class SpotifyToken
{
    #region JSON Attributes
    public string access_token;
    public string token_type;
    public string scope;
    public int expires_in;
    #endregion

    public static SpotifyToken CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<SpotifyToken>(jsonString);
    }
}

public class SpotifyTrack
{
    #region JSON Attributes
    public string access_token;
    public string token_type;
    public string scope;
    public int expires_in;
    #endregion

    public static SpotifyTrack CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<SpotifyTrack>(jsonString);
    }
}
#endregion