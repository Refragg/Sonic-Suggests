using UnityEngine;

public class PlayerCharacter : MonoBehaviour, ISpringActivator, IPlayerHittable, IPickUpCollector, IDrownable {

    private Collider[] colliders = new Collider[32];

    public MovingCharacter character;
    public CharacterAnimator animator;
    public float jumpVelocity;
    public AudioSource audioSource;
    public int ringCount;
    public Renderer characterRenderer;
    public GameObject droppedRingPrefab;
    public GameObject respawnPoint;

    public AudioClip jumpSound;
    public AudioClip ringLossSound;

    private bool isJumping;
    private bool jump;
    private Vector2 direction;
    private float invisibilityTime;

    private void Update() {
        UpdateMovement();
        CheckForHittable();
        UpdateInvisibilityAnim();
    }

    private void UpdateInvisibilityAnim() {
        invisibilityTime -= Time.deltaTime;
        if(IsInvincible) {
            animator.hurt = true;
            characterRenderer.enabled = Mathf.PingPong(Time.time * 30, 1) > 0.25;
        } else {
            animator.hurt = false;
            characterRenderer.enabled = true;
        }
    }

    private void CheckForHittable() {
        if(isJumping) {

            var center = character.characterCapsule.transform.TransformPoint(
                    character.characterCapsule.center) + Vector3.down * character.characterCapsule.height / 1.5f;

            var count = Physics.OverlapSphereNonAlloc(center, character.characterCapsule.radius * 1.5f, colliders);

            for(int i = 0; i < count; i++) {
                var c = colliders[i];

                if(c.isTrigger) {
                    continue;
                }

                IHittable hittable = c.gameObject.GetComponent<IHittable>();

                if(hittable != null) {

                    if(c.transform.position.y < transform.position.y && character.characterRigidbody.velocity.y < 0) {
                        var v = character.characterRigidbody.velocity;
                        v.y = -v.y * 1.2f;
                        character.characterRigidbody.velocity = v;
                    }

                    hittable.OnHit(this);
                }
            }
        }
    }

    private void UpdateMovement() {
        var v = character.characterRigidbody.velocity;

        if(character.OnGround && isJumping && v.y < 0.1) {
            isJumping = false;
            animator.Normal();
        }

        if(!isJumping && character.OnGround && jump) {
            v.y = jumpVelocity;
            character.characterRigidbody.velocity = v;
            isJumping = true;
            animator.Jump();
            audioSource.PlayOneShot(jumpSound);
        }

        character.SetVelocity(direction);
        jump = false;
    }

    public void SetDirection(Vector2 rotation) {
        direction = rotation;
    }

    public void Jump() {
        jump = true;
    }

    public bool IsInvincible { get { return invisibilityTime >= 0; }}

    public void OnSpringActivated(SpringObject spring) {
        var vector = spring.transform.up * spring.springForce;
        character.characterRigidbody.velocity = vector;
    }

    public void OnPlayerHit(GameObject enemy, bool ignoreProtection) {
        if(invisibilityTime <= 0) {
            if(ringCount >= 0) {
                for(int i = 0; i < ringCount; i++) {
                    var obj = Instantiate(droppedRingPrefab);
                    var body = obj.GetComponent<Rigidbody>();
                    var rotation = Quaternion.Euler(0, i / (float) ringCount * 360, 0);

                    obj.transform.position = transform.position
                    + new Vector3(0, 0.5f, 0)
                    + rotation * Vector3.forward;

                    body.velocity = rotation * new Vector3(0f, 5f, 2f);
                    body.rotation = rotation;
                }

                audioSource.PlayOneShot(ringLossSound);
                invisibilityTime = 5f;
                ringCount = 0;
            } else {
                // dead
            }
        }
    }

    public void OnPickUpCollected(GameObject pickUpObject) {
        if(pickUpObject.GetComponent<Ring>()) {
            ringCount++;
        }
    }

    public void OnDrown()
    {
        Kill();
    }

    private void Kill()
    {
        
    }
}
