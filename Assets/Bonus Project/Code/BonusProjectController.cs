using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BonusProjectController : MonoBehaviour {

    private const int GrassDensity = 50;

    // Settings
    public float MoveSpeed = 30;
    public float SteerAngle = 20;
    public float Drag = 0.98f;
    public float MaxSpeed = 10;
    public float Traction = 1;
    public float PerlinScale = 1.2f;

    // References
    public GameObject LeftTire;
    public GameObject RightTire;
    public GameObject PrefabGrassBlade;

    // Variables
    private Vector3 MoveForce;
    private GameObject[,] GrassBladeList = new GameObject[GrassDensity, GrassDensity];
    private int CutBladesCount;
    private int AllBladesCount;
    private float TimeSinceCut;

    // Start is called before the first frame update
    void Start() {
        for (int i = 0; i < GrassDensity; i++) {
            for (int j = 0; j < GrassDensity; j++) {
                GameObject blade = Instantiate(PrefabGrassBlade);
                float x = Mathf.Lerp(-10, 10, i / (float)GrassDensity);
                float z = Mathf.Lerp(-10, 10, j / (float)GrassDensity);
                x += Random.value * 0.25f;
                z += Random.value * 0.25f;
                blade.transform.position = new Vector3(x, 0, z);
                GrassBladeList[i, j] = blade;
                AllBladesCount++;
            }
        }
    }

    // Update is called once per frame
    void Update() {

        if (Input.GetKeyDown(KeyCode.Space)) {
            ResetGrass();
            ResetCar();
        }

        // Update force,
        // and move in the direction of the force
        float input = Input.GetAxis("Vertical");
        MoveForce += transform.forward * MoveSpeed * Input.GetAxis("Vertical") * Time.deltaTime;
        transform.position += MoveForce * Time.deltaTime;

        // Slowdown with drag
        MoveForce *= Drag;


        // Max speed
        MoveForce = Vector3.ClampMagnitude(MoveForce, MaxSpeed);

        // Gradually align MoveForce in forward direction, simulating traction
        float r = Mathf.Lerp(5, 1, Mathf.InverseLerp(0, 1, input));
        MoveForce = Vector3.Lerp(MoveForce.normalized, transform.forward, Time.deltaTime * Traction * r) * MoveForce.magnitude;

        // Rotate tires
        float steerInput = Input.GetAxis("Horizontal");
        Quaternion tireRotation = Quaternion.Euler(Vector3.up * steerInput * 30);
        LeftTire.transform.localRotation = tireRotation;
        RightTire.transform.localRotation = tireRotation;

#if UNITY_EDITOR
        Debug.DrawRay(transform.position, MoveForce, new Color(1, 1, 1, 0.5f));
        Debug.DrawRay(transform.position, MoveForce.normalized * 3);
        Debug.DrawRay(transform.position, transform.forward * 3, Color.blue);
#endif

        float sideForce = Vector3.Dot(MoveForce.normalized, transform.right);
        float angle = -Mathf.Pow(sideForce, 5f) * 45;

        transform.GetChild(0).localRotation = Quaternion.Euler(Vector3.forward * angle);

        transform.GetChild(0).transform.localPosition = Vector3.up * Mathf.Lerp(0, 0.5f, Mathf.Abs(sideForce))
            + Vector3.up * Mathf.Abs(Mathf.Sin(Time.time * 3.14f * 5)) * 0.1f * Mathf.InverseLerp(5, 3, MoveForce.magnitude);

        // Steer car
        transform.Rotate(Vector3.up * steerInput * MoveForce.magnitude * SteerAngle * Time.deltaTime);

        // Update grass blades
        for (int i = 0; i < GrassDensity; i++) {
            for (int j = 0; j < GrassDensity; j++) {
                float x = i * PerlinScale + Time.time;
                float z = j * PerlinScale + Time.time;
                float perlinValue = Mathf.PerlinNoise(x, z);
                GrassBladeList[i, j].transform.rotation = Quaternion.Euler(Vector3.forward * Mathf.Lerp(-30, 30, perlinValue));
            }
        }
    }

    private void OnTriggerEnter(Collider other) {
        other.gameObject.SetActive(false);

        // Reset grass if 80% of it was cut
        CutBladesCount++;
        if (CutBladesCount > AllBladesCount * 0.8f) {
            // Reset grass
            ResetGrass();
        }
    }

    private void ResetGrass() {
        CutBladesCount = 0;
        for (int i = 0; i < GrassDensity; i++) {
            for (int j = 0; j < GrassDensity; j++) {
                GameObject blade = GrassBladeList[i, j];
                blade.transform.GetChild(0).gameObject.SetActive(true);
            }
        }
    }

    private void ResetCar() {
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        MoveForce = Vector3.zero;
        foreach (var item in transform.GetComponentsInChildren<TrailRenderer>()) {
            item.Clear();
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos() {
        float sideForce = Vector3.Dot(MoveForce.normalized, transform.right);
        Handles.Label(new Vector3(0, 0, -12), TimeSinceCut.ToString("F1"));
    }
#endif
}
