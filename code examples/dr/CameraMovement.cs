using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class CameraMovement: MonoBehaviour
{
    // Attach this to the main camera.
    
    private Vector2 originalPosition;
    private SceneManager sceneManager;
    public SfxManager SfxManager;
    
    private Coroutine activeCoroutine;

    private float baseZoom;
    private float zoom;

    private bool zoomed;

    private Camera cam;
    private float zoomSpeed = 0.2f;

    private float shakeMagnitude = 3.5f;
    private const int defaultFrameShake = 40;
    
    public bool Zooming { get; set; }
    
    private void Start()
    {
        originalPosition = transform.position;
        sceneManager = GameObject.Find("Canvas").GetComponent<SceneManager>();
        cam = GetComponent<Camera>();
        baseZoom = cam.orthographicSize;
        zoom = baseZoom - 30f;
    }
    
    public void ShakeCamera(int frames=defaultFrameShake)
    {
        StartCoroutine(ShakeEffect());
    }

    IEnumerator ShakeEffect(int frames=defaultFrameShake)
    {
        var frame = 0;
        var originalPosition = cam.transform.position;

        while (frame < frames)
        {
            while (Game.IsPaused)
            {
                yield return null;
            }
            var xOffset = Random.Range(0f, 1f) * shakeMagnitude * 2 - shakeMagnitude;
            var yOffset = Random.Range(0f, 1f) * shakeMagnitude * 2 - shakeMagnitude;
            cam.transform.position += new Vector3(xOffset, yOffset, 0);
            frame += 1;
            yield return null;
        }

        cam.transform.position = originalPosition;
        yield return null;
    }

    // When an object is inspected, zoom in on it slightly.
    public void MoveToObject(SceneObject sceneObject)
    {
        // Only zoom if it isn't an initial dialogue.
        if (sceneObject.Name != "Initial")
        {
            ForceCoroutine(Move(sceneObject.Position));
            if (!zoomed)
            {
                StartCoroutine(ZoomIn());

            }
        }
        
    }

    public void MoveToObjectInstant(SceneObject sceneObject)
    {
        if (sceneObject.Name != "Initial")
        {
            StopActiveCoroutine();
            transform.position = sceneObject.Position;
        }
    }
    
    public void ReturnToOriginalPosition()
    {
        ForceCoroutine(Move(originalPosition));
        StartCoroutine(ZoomOut());
    }
    
    public void ReturnToOriginalPositionInstant()
    {
        StopActiveCoroutine();
        transform.position = originalPosition;
    }

    public void ZoomInInstant()
    {
        zoomed = true;
        cam.orthographicSize = zoom;
    }
    
    public void ZoomOutInstant()
    {
        zoomed = false;
        cam.orthographicSize = baseZoom;
    }
    
    public void MoveToCharacter(CharacterID characterId)
    {
        var character = sceneManager.CurrentScene.GetCharacterObject(characterId);
        ForceCoroutine(Move(character.Position));
        if (!zoomed)
        {
            StartCoroutine(ZoomIn());

        }
    }

    public void MoveToCharacterInstant(CharacterID characterId)
    {
        StopActiveCoroutine();
        var character = sceneManager.CurrentScene.GetCharacterObject(characterId);
        transform.position = character.Position;
    }

    private void ForceCoroutine(IEnumerator coroutine)
    {
        StopActiveCoroutine();
        activeCoroutine = StartCoroutine(coroutine);
    }

    private void StopActiveCoroutine()
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            //cam.orthographicSize = baseZoom;
        }
    }

    IEnumerator ZoomIn()
    {
        Zooming = true;
        
        SfxManager.PlaySound(SfxID.Zoom);
        while (cam.orthographicSize > zoom + 0.01f)
        {
            if (Game.IsPaused)
            {
                yield return null;
            }
            cam.orthographicSize = Mathf.SmoothStep(cam.orthographicSize, zoom, zoomSpeed);
            yield return null;
        }

        zoomed = true;
        Zooming = false;
        cam.orthographicSize = zoom;

        yield return null;

    }
    
    IEnumerator ZoomOut()
    {
        Zooming = true;

        while (cam.orthographicSize < baseZoom - 0.01f)
        {
            if (Game.IsPaused)
            {
                yield return null;
            }
            cam.orthographicSize = Mathf.SmoothStep(cam.orthographicSize, baseZoom, zoomSpeed);
            yield return null;
        }

        zoomed = false;
        Zooming = false;
        cam.orthographicSize = baseZoom;

        yield return null;

    }

    IEnumerator Move(Vector2 newPosition)
    {
        while (Vector2.Distance(transform.position, newPosition) > 2)
        {
            if (Game.IsPaused)
            {
                yield return null;
            }
            transform.position = Vector2.Lerp(transform.position, newPosition, 0.08f);
                        
            yield return null;

        }

        yield return null;
    }

}
