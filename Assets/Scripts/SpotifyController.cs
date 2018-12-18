using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.UI;
using Vuforia;

public class SpotifyController : MonoBehaviour, ITrackableEventHandler
{

    #region Private attributes
    private string refresh_token = "AQB_BRrIRHy4kYgc42srVPz2hIJVLYo2TuOBswPK4foCx2VfZ1leH4zBZV4qJdanDQ_CfEe8wqzwp-ETFlEXzmcg-ANb5tclnO2NVyhGzMlv5dW03fSLdLYwRsiNnrSNhbGyMg";
    private string encoded = "Basic ZjIyYzI5ZTQ3ODdlNGExODlmNWRhZTFmMDdkZjY2ZTM6ZGE3MmUwYWNjNzM2NDgwNGIxNTNjMDQyNTRmYjVhNDQ=";
    private string uri = "https://accounts.spotify.com/api/token";
    private bool refreshingToken = true;
    private bool requestingTrackInfo = false;
    private Slider slider;
    private UnityEngine.UI.Image image;
    private TrackableBehaviour mTrackableBehaviour;
    private Animator animator;
    private Animator clockAnimator;
    private Animator displendAnimator;
    #endregion

    #region Public attributes
    public float requestCooldown;
    public Text trackName;
    public Text artistName;
    public Text albumName;
    #endregion

    void Start()
    {
        slider = GameObject.Find("Slider").GetComponent<Slider>();
        image = GameObject.Find("Album cover").GetComponent<UnityEngine.UI.Image>();

        mTrackableBehaviour = GetComponent<TrackableBehaviour>();
        if (mTrackableBehaviour)
        {
            mTrackableBehaviour.RegisterTrackableEventHandler(this);
        }

        animator = GameObject.Find("Spotify").GetComponent<Animator>();
        clockAnimator = GameObject.Find("Clock").GetComponent<Animator>();
        displendAnimator = GameObject.Find("displend_logo").GetComponent<Animator>();
    }

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
                ExtractTrack(www.downloadHandler.text);

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

    IEnumerator DownloadAlbumImage(SpotifyImage[] images)
    {
        /* Prioritize the highest resolution image */
        SpotifyImage selected = images[0];
        if (selected == null) selected = images[1];
        if (selected == null) selected = images[2];

        /* Download image */
        string url = selected.url;
        using (WWW www = new WWW(url))
        {
            /*  Wait for download to complete */
            yield return www;

            /* Create a texture in DXT1 format */
            Texture2D texture = new Texture2D(www.texture.width, www.texture.height, TextureFormat.DXT1, false);

            /* Assign the downloaded image to sprite */
            www.LoadImageIntoTexture(texture);
            Rect rec = new Rect(0, 0, texture.width, texture.height);
            Sprite spriteToUse = Sprite.Create(texture, rec, new Vector2(0.5f, 0.5f), 100);
            image.sprite = spriteToUse;
        }
    }

    void ExtractTrack(string response)
    {
        SpotifyItem item = SpotifyItem.CreateFromJSON(response);

        /* If no track is being listened */
        if (item == null)
        {
            trackName.text = "No track selected";
            albumName.text = "";
            artistName.text = "";
        }
        else
        {
            SpotifyTrack track = item.item;

            /* Setting the current value of the progress bar */
            slider.value = item.progress_ms;

            /* Check to see if track has changed */
            if (trackName.text != track.name)
            {
                trackName.text = track.name;
                artistName.text = "by " + track.artists[0].name;

                /* Setting the max value of the progress bar */
                slider.maxValue = track.duration_ms;

                /* If album is a new one, download image and change image */
                if (track.album.name != albumName.text)
                {
                    albumName.text = track.album.name;
                    StartCoroutine(DownloadAlbumImage(track.album.images));
                }
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

    public void OnTrackableStateChanged(
                                    TrackableBehaviour.Status previousStatus,
                                    TrackableBehaviour.Status newStatus)
    {

        if (newStatus == TrackableBehaviour.Status.DETECTED ||
            newStatus == TrackableBehaviour.Status.TRACKED ||
            newStatus == TrackableBehaviour.Status.EXTENDED_TRACKED)
        {
            animator.SetBool("detected", true);
            clockAnimator.SetBool("detected", true);
            displendAnimator.SetBool("detected", true);
        }
        else if (previousStatus == TrackableBehaviour.Status.TRACKED &&
                newStatus == TrackableBehaviour.Status.NOT_FOUND)
        {
            animator.SetBool("detected", false);
            clockAnimator.SetBool("detected", false);
            displendAnimator.SetBool("detected", false);
        }
    }
}

#region JSON classes
[System.Serializable]
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

[System.Serializable]
public class SpotifyImage
{
    #region JSON Attributes
    public int height;
    public int width;
    public string url;
    #endregion

    public static SpotifyImage CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<SpotifyImage>(jsonString);
    }
}

[System.Serializable]
public class SpotifyArtist
{
    #region JSON Attributes
    public string name;
    #endregion

    public static SpotifyArtist CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<SpotifyArtist>(jsonString);
    }
}

[System.Serializable]
public class SpotifyAlbum
{
    #region JSON Attributes
    public string name;
    public SpotifyImage[] images;
    #endregion

    public static SpotifyAlbum CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<SpotifyAlbum>(jsonString);
    }
}

[System.Serializable]
public class SpotifyTrack
{
    #region JSON Attributes
    public SpotifyAlbum album;
    public SpotifyArtist[] artists;
    public string name;
    public int duration_ms;
    public bool is_playing;
    #endregion

    public static SpotifyTrack CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<SpotifyTrack>(jsonString);
    }
}

[System.Serializable]
public class SpotifyItem
{
    #region JSON Attributes
    public SpotifyTrack item;
    public int progress_ms;
    #endregion

    public static SpotifyItem CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<SpotifyItem>(jsonString);
    }
}
#endregion