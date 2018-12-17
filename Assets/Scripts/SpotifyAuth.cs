using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;

public class SpotifyAuth : MonoBehaviour
{

    #region Private attributes
    private string refresh_token = "AQB_BRrIRHy4kYgc42srVPz2hIJVLYo2TuOBswPK4foCx2VfZ1leH4zBZV4qJdanDQ_CfEe8wqzwp-ETFlEXzmcg-ANb5tclnO2NVyhGzMlv5dW03fSLdLYwRsiNnrSNhbGyMg";
    private string encoded = "Basic ZjIyYzI5ZTQ3ODdlNGExODlmNWRhZTFmMDdkZjY2ZTM6ZGE3MmUwYWNjNzM2NDgwNGIxNTNjMDQyNTRmYjVhNDQ=";
    private string uri = "https://accounts.spotify.com/api/token";
    private bool refreshingToken = true;
	private bool requestingTrackInfo = false;

	public float requestLoopTime;

    #endregion

	void Update()
	{
		if (!requestingTrackInfo)
		{
			StartCoroutine(GetTrackInfo());
		}
	}

	IEnumerator GetTrackInfo()
    {
		requestingTrackInfo = true;

		StartCoroutine(RefreshToken());
		while (refreshingToken)
		{
			yield return new WaitForSeconds(0.1f);
		}

        string url = "https://api.spotify.com/v1/me/player";
        string auth = "Bearer " + PlayerPrefs.GetString("access_token");

        using (UnityWebRequest uwr = UnityWebRequest.Get(url))
        {
            uwr.SetRequestHeader("Authorization", auth);
            yield return uwr.Send();

            if (uwr.isNetworkError || uwr.isHttpError)
            {
                Debug.Log(uwr.error);
            }
            else
            {
                Debug.Log(uwr.downloadHandler.text);

				yield return new WaitForSeconds(requestLoopTime);
				requestingTrackInfo = false;
            }
        }
    }

    IEnumerator RefreshToken()
    {
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
                Debug.Log("Error!");
                Debug.Log(www.error);
                Debug.Log(www.responseCode);
            }
            else
            {
                Debug.Log("Spotify token refreshed");
                string response = www.downloadHandler.text;
                SetToken(response);

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
    }
}

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
