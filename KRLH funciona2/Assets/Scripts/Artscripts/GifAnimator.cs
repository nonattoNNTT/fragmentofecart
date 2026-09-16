using UnityEngine;
using UnityEngine.UI;

public class GifAnimator : MonoBehaviour
{
    public Image image;
    public Sprite[] frames;
    public float frameRate = 12f;

    private int currentFrame = 0;
    private float timer = 0f;

    private void Update()
    {
        if (frames.Length == 0)
            return;

        timer += Time.deltaTime;

        if (timer >= 1f / frameRate)
        {
            timer = 0f;

            currentFrame++;

            if (currentFrame >= frames.Length)
                currentFrame = 0;

            image.sprite = frames[currentFrame];
        }
    }
}