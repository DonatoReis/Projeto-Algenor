using UnityEngine;

public class FootSensor : MonoBehaviour
{
    public bool isGrounded { get; private set; }
    public float lastContactTime { get; private set; }
    public float contactNormalForce { get; private set; }
    public Vector3 contactPoint { get; private set; }
    public Vector3 contactNormal { get; private set; }

    private void Start()
    {
        lastContactTime = Time.time;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            lastContactTime = Time.time;
            UpdateContactInfo(collision);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            UpdateContactInfo(collision);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
            contactNormalForce = 0f;
            contactPoint = Vector3.zero;
            contactNormal = Vector3.up;
        }
    }

    private void UpdateContactInfo(Collision collision)
    {
        ContactPoint contact = collision.GetContact(0);
        contactPoint = contact.point;
        contactNormal = contact.normal;
        contactNormalForce = collision.impulse.magnitude / Time.fixedDeltaTime;
    }

    private void OnDrawGizmos()
    {
        if (isGrounded)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(contactPoint, 0.05f);
            Gizmos.DrawRay(contactPoint, contactNormal * 0.5f);
        }
    }
}