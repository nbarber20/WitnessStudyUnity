using UnityEngine;

/*
    Simple Player Controller Class
*/

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private float m_moveSpeed = 7.5f;
    [SerializeField]
    private float m_lookSpeed = 2.0f;
    [SerializeField]
    private float m_lookXLimit = 45.0f;
    [SerializeField]
    private float m_mouseResetDeadzone = 0.1f;
    [SerializeField]
    private GameObject m_uiBorder = null;

    private CharacterController m_characterController = null;
    private Camera m_playerCamera = null;
    private float m_rotationX = 0;
    private bool m_canMove = true;
    private bool m_reset = false;

    private void Awake()
    {
        m_characterController = GetComponent<CharacterController>();
        m_playerCamera = GetComponentInChildren<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SetInputEnabled(true);
    }

    private void Update()
    {
        if (!m_canMove) return;

        //Movement
        Vector2 inputDir = new Vector2(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal")) * m_moveSpeed;
        Vector3 moveDir = (transform.forward * inputDir.x) + (transform.right * inputDir.y);
        m_characterController.Move(moveDir * Time.deltaTime);

        //Rotation
        Vector2 mouseInput = new Vector2(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"));
        if(mouseInput.magnitude < m_mouseResetDeadzone)
        {
            m_reset = true; //Quick fix for annoying issues when transitions from puzzle type input to mouse look input
        }
        if (m_reset)
        {
            m_rotationX += mouseInput.y * m_lookSpeed;
            m_rotationX = Mathf.Clamp(m_rotationX, -m_lookXLimit, m_lookXLimit);
            m_playerCamera.transform.localRotation = Quaternion.Euler(m_rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, mouseInput.x * m_lookSpeed, 0);
        }

        //Interaction
        if(Input.GetButtonDown("Fire1"))
        {
            Ray r = new Ray(m_playerCamera.transform.position, m_playerCamera.transform.forward);
            if (Physics.Raycast(r, out RaycastHit hit))
            {
                LinePuzzle puzzle = hit.transform.GetComponent<LinePuzzle>();
                if (puzzle != null)
                {
                    puzzle.Focus(this);
                }
            }
        }
    }

    public void SetInputEnabled(bool v)
    {
        m_uiBorder.SetActive(!v);
        m_canMove = v;
        m_reset = false;
    }
}