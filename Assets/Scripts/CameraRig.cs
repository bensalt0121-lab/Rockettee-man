/*********************************************************************************************
 * COMPONENT OF: Main Camera (added by the game)
 * REQUIRED DEPENDENCIES: an orthographic camera, RocketeeGame
 * DESCRIPTION: Moves the camera for each part of the game: follows the player in the yard,
 *              zooms in on the build grid, and follows the rocket up into space. The faster
 *              the rocket goes, the more it zooms out (feels fast and you see meteors coming).
 *              CameraRig.Shake() shakes the screen for hits, launches and crashes.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.1
 * VERSION 1.1: Zooms out with speed, screen shake.
 *********************************************************************************************/
using UnityEngine;

public class CameraRig : MonoBehaviour
{
    public float yardSize = 5f;
    public float flightSize = 7.5f;
    // Extra zoom out per meter per second of rocket speed, and the most it zooms out
    public float speedZoom = 0.07f;
    public float maxSpeedZoom = 4f;
    // How high above the ground the camera looks in the yard
    public float yardHeight = 3.2f;
    public float smoothTime = 0.15f;

    private static float shakeStrength;
    private static float shakeTimeLeft;
    private const float ShakeLength = 0.35f;

    private Camera cam;
    private Vector3 velocity;
    private float sizeVelocity;
    // Where the camera is without the shake
    private Vector3 steadyPosition;

    public static CameraRig Attach()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
            go.transform.position = new Vector3(0f, 0f, -10f);
        }

        cam.orthographic = true;
        CameraRig rig = cam.GetComponent<CameraRig>();
        if (rig == null)
            rig = cam.gameObject.AddComponent<CameraRig>();
        rig.cam = cam;
        rig.steadyPosition = cam.transform.position;
        return rig;
    }

    // Shakes the screen; bigger number = stronger (0.3 is a hit, 0.6 is a crash)
    public static void Shake(float strength)
    {
        shakeStrength = Mathf.Max(shakeTimeLeft > 0f ? shakeStrength : 0f, strength);
        shakeTimeLeft = ShakeLength;
    }

    void LateUpdate()
    {
        RocketeeGame game = RocketeeGame.Instance;
        if (game == null || cam == null)
            return;

        Vector2 target = steadyPosition;
        float size = yardSize;
        float smooth = smoothTime;

        switch (game.State)
        {
            case GameState.Yard:
            case GameState.Shop:
                target = YardTarget(game);
                break;

            case GameState.Build:
                // The grid sits in the upper part of the screen so the build bar at the
                // bottom never covers the lowest row
                float grid = game.Data.GridSize;
                size = Mathf.Max(yardSize, (grid + 1f) / 1.32f + 0.5f);
                target = game.Pad.GridOrigin + Vector2.up * (0.32f * size);
                break;

            case GameState.Flight:
            case GameState.Results:
            case GameState.Won:
                if (game.Rocket != null)
                {
                    target = (Vector2)game.Rocket.transform.position + Vector2.up * 2.5f;
                    size = flightSize + Mathf.Min(maxSpeedZoom, game.Rocket.Body.linearVelocity.magnitude * speedZoom);
                }
                else
                {
                    size = flightSize;
                }
                // Follows a fast rocket closely so it never leaves the screen
                smooth = 0.05f;
                break;
        }

        Vector3 goal = new Vector3(target.x, target.y, steadyPosition.z);
        steadyPosition = Vector3.SmoothDamp(steadyPosition, goal, ref velocity, smooth);
        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, size, ref sizeVelocity, 0.3f);

        // Screen shake on top of the steady position, fading out
        Vector3 shake = Vector3.zero;
        if (shakeTimeLeft > 0f)
        {
            shakeTimeLeft -= Time.unscaledDeltaTime;
            shake = (Vector3)(Random.insideUnitCircle * shakeStrength * Mathf.Clamp01(shakeTimeLeft / ShakeLength));
        }
        transform.position = steadyPosition + shake;
    }

    // Follows the player left and right but never shows past the edge of the yard
    Vector2 YardTarget(RocketeeGame game)
    {
        float x = game.Player != null ? game.Player.transform.position.x : 0f;
        float halfWidth = yardSize * cam.aspect;
        float left = game.YardLeft + halfWidth;
        float right = game.YardRight - halfWidth;

        x = left <= right ? Mathf.Clamp(x, left, right) : (game.YardLeft + game.YardRight) * 0.5f;
        return new Vector2(x, game.GroundY + yardHeight);
    }

    // Jumps straight to the target (used at the start and after a flight)
    public void Snap()
    {
        velocity = Vector3.zero;
        RocketeeGame game = RocketeeGame.Instance;
        if (game == null || cam == null)
            return;

        Vector2 target = YardTarget(game);
        steadyPosition = new Vector3(target.x, target.y, transform.position.z);
        transform.position = steadyPosition;
        cam.orthographicSize = yardSize;
    }
}
