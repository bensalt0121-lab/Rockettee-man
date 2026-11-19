/*********************************************************************************************
 * COMPONENT OF: Main Camera (added by the game)
 * REQUIRED DEPENDENCIES: an orthographic camera, RocketeeGame
 * DESCRIPTION: Moves the camera for each part of the game: follows the player in the yard,
 *              zooms in on the build grid, and follows the rocket up into space (zoomed out
 *              a bit so you can see meteors coming).
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.0
 *********************************************************************************************/
using UnityEngine;

public class CameraRig : MonoBehaviour
{
    public float yardSize = 5f;
    public float flightSize = 7.5f;
    // How high above the ground the camera looks in the yard
    public float yardHeight = 3.2f;
    public float smoothTime = 0.15f;

    private Camera cam;
    private Vector3 velocity;
    private float sizeVelocity;

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
        return rig;
    }

    void LateUpdate()
    {
        RocketeeGame game = RocketeeGame.Instance;
        if (game == null || cam == null)
            return;

        Vector2 target = transform.position;
        float size = yardSize;
        float smooth = smoothTime;

        switch (game.State)
        {
            case GameState.Yard:
            case GameState.Shop:
                target = YardTarget(game);
                break;

            case GameState.Build:
                float grid = game.Data.GridSize;
                target = game.Pad.GridOrigin + Vector2.up * (grid * 0.5f);
                size = Mathf.Max(yardSize, grid * 0.5f + 2f);
                break;

            case GameState.Flight:
            case GameState.Results:
            case GameState.Won:
                if (game.Rocket != null)
                    target = (Vector2)game.Rocket.transform.position + Vector2.up * 2.5f;
                size = flightSize;
                // Follows a fast rocket closely so it never leaves the screen
                smooth = 0.05f;
                break;
        }

        Vector3 goal = new Vector3(target.x, target.y, transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smooth);
        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, size, ref sizeVelocity, 0.3f);
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
        transform.position = new Vector3(target.x, target.y, transform.position.z);
        cam.orthographicSize = yardSize;
    }
}
