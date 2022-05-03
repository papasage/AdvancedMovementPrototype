using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Pitch/Volume Random Values")]
    [SerializeField] float randomMin;
    [SerializeField] float randomMax;

    [Header("Sounds")]
    [SerializeField] AudioClip walk;
    [SerializeField] AudioClip sprint;
    [SerializeField] AudioClip jump;
    [SerializeField] AudioClip doublejump;
    [SerializeField] AudioClip slide;

    public AudioSource playersound;
    // Start is called before the first frame update
    void Start()
    {
        playersound = GetComponent<AudioSource>();
    }

    public void PlayWalk()
    {
        playersound.volume = .5f;
        playersound.pitch = Random.Range(randomMin, randomMax);
        playersound.PlayOneShot(walk);
    } 
    public void PlaySprint()
    {
        playersound.volume = .6f;
        playersound.pitch = Random.Range(randomMin, randomMax);
        playersound.PlayOneShot(sprint);
    }
    public void PlayJump()
    {
        playersound.volume = Random.Range(randomMin, randomMax);
        playersound.pitch = Random.Range(randomMin, randomMax);
        playersound.PlayOneShot(jump);
    }
    public void PlayDoubleJump()
    {
        playersound.volume = .5f;
        playersound.pitch = Random.Range(randomMin, randomMax);
        playersound.PlayOneShot(doublejump);
    }
    public void PlaySlide()
    {
        playersound.clip = slide;
        playersound.volume = .6f;
        playersound.pitch = Random.Range(randomMin, randomMax);
        playersound.Play();
        //playersound.PlayOneShot(slide);
    }

}
