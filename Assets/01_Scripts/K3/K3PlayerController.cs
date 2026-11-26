using UnityEngine;

/// <summary>
/// First Person Controller untuk Simulasi Ruang K3 (Praktikum M01).
/// Mendukung navigasi WASD + Mouse Look di desktop serta deteksi zona keselamatan K3.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class K3PlayerController : MonoBehaviour
{
    [Header("Identitas Mahasiswa")]
    public string namaMahasiswa = "Ardan Rizqi P";
    public string nimMahasiswa = "0920240011";

    [Header("Pergerakan")]
    [SerializeField] private float _walkSpeed = 4.0f;
    [SerializeField] private float _runSpeed = 6.5f;
    [SerializeField] private float _gravity = -18.0f;

    [Header("Kamera")]
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private float _mouseSensitivity = 2.0f;
    [SerializeField] private float _minPitch = -80f;
    [SerializeField] private float _maxPitch = 80f;

    private CharacterController _controller;
    private float _pitch = 0f;
    private float _verticalVelocity = 0f;
    private string _currentZoneName = "Zona Jalur Aman (Safe Walkway)";
    private Color _zoneColor = Color.green;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        if (_cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) _cameraTransform = cam.transform;
        }

        // Kunci kursor saat klik game view
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        HandleMouseLook();
        HandleMovement();
        EvaluateSafetyZone();
    }

    private void HandleMouseLook()
    {
        if (Input.GetMouseButton(1) || Cursor.lockState == CursorLockMode.Locked)
        {
            float mouseX = Input.GetAxis("Mouse X") * _mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * _mouseSensitivity;

            transform.Rotate(Vector3.up * mouseX);

            _pitch -= mouseY;
            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);

            if (_cameraTransform != null)
            {
                _cameraTransform.localEulerAngles = new Vector3(_pitch, 0f, 0f);
            }
        }

        // Toggle kursor dengan tombol Escape / Tab
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Cursor.lockState = (Cursor.lockState == CursorLockMode.Locked) ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = (Cursor.lockState != CursorLockMode.Locked);
        }
    }

    private void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isRunning ? _runSpeed : _walkSpeed;

        Vector3 moveDirection = transform.forward * v + transform.right * h;
        if (moveDirection.sqrMagnitude > 1f) moveDirection.Normalize();

        if (_controller.isGrounded)
        {
            _verticalVelocity = -1f;
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _verticalVelocity = 4.5f;
            }
        }
        else
        {
            _verticalVelocity += _gravity * Time.deltaTime;
        }

        Vector3 velocity = moveDirection * currentSpeed;
        velocity.y = _verticalVelocity;

        _controller.Move(velocity * Time.deltaTime);
    }

    private void EvaluateSafetyZone()
    {
        Vector3 pos = transform.position;

        // Zona Mesin Press (Z > 0 dan dekat mesin)
        if (pos.z > 0.0f && Mathf.Abs(pos.x) < 6.5f)
        {
            _currentZoneName = "AREA BAHAYA: Zona Mesin Press (Wajib APD & Hati-hati!)";
            _zoneColor = new Color(1f, 0.3f, 0.2f);
        }
        // Zona Tanggap Darurat & Exit (Dekat dinding timur atau APAR)
        else if (pos.x > 5.0f && pos.z < 0.0f)
        {
            _currentZoneName = "ZONA TANGGAP DARURAT: Titik APAR & Jalur Evakuasi Exit";
            _zoneColor = new Color(0.2f, 0.8f, 1f);
        }
        // Zona Jalur Aman (Walkway)
        else
        {
            _currentZoneName = "ZONA AMAN: Jalur Pejalan Kaki (Safe Pedestrian Walkway)";
            _zoneColor = new Color(0.3f, 1f, 0.4f);
        }
    }

    private void OnGUI()
    {
        // Tampilan HUD Informasi K3
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.fontSize = 13;
        boxStyle.alignment = TextAnchor.UpperLeft;

        GUILayout.BeginArea(new Rect(15, 15, 480, 130), boxStyle);
        GUILayout.Label($"<b>SIMULASI K3 WORKSHOP & AREA MESIN PRESS</b>");
        GUILayout.Label($"Mahasiswa : <b>{namaMahasiswa}</b> | NIM : <b>{nimMahasiswa}</b>");
        GUILayout.Label($"Kontrol   : WASD = Jalan | Shift = Lari | Klik Kanan/Tab = Rotasi Kamera");
        
        GUIStyle statusStyle = new GUIStyle(GUI.skin.label);
        statusStyle.fontSize = 13;
        statusStyle.fontStyle = FontStyle.Bold;
        statusStyle.normal.textColor = _zoneColor;

        GUILayout.Label($"Status K3 : {_currentZoneName}", statusStyle);
        GUILayout.EndArea();
    }
}
