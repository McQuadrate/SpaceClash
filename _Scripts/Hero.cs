using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ActorSound
{
    public string name;
    public AudioClip clip;
}

public class Hero : MonoBehaviour
{
    static public Hero S;

    [Header("Voice")]

    public ActorSound[] powerUpSounds;
    public ActorSound[] loseSounds;
    public ActorSound[] attackSounds;
    public float chanceToAttackSound = 0.3f;
    public AudioSource audioSource;
    public ActorSound weaponSound;
    
    [Header("set in Inspector")]

    public float speed = 30;
    public float rollMult = -45;
    public float pitchMult = 30;
    public float gameRestartDelay = 2f;

    public GameObject projectilePrefab;
    public float projectileSpeed = 40;

    public Weapon[] weapons;

    [Header("Set Dynamically")]

    [SerializeField] private float _shieldLevel = 1;

    private GameObject lastTriggerGo = null;

    public delegate void WeaponFireDelegate();
    public WeaponFireDelegate fireDelegate;

    private void Start()
    {
        
        if(S == null)
        {
            S = this;
            deltaTimeFire = fireRate + 0.1f;
            ClearWeapons();
            weapons[0].SetType(WeaponType.blaster);
            audioSource = GetComponent<AudioSource>();
        }
        else
        {
            Debug.LogError("Hero.Awake() - Attemped to assign secon Hero.S");
        }
        //fireDelegate += TempFire;

    }


   [SerializeField] public  float fireRate = 1f;
    private float deltaTimeFire;
    private void Update()
    {
        float xAxis = Input.GetAxis("Horizontal");
        float yAxis = Input.GetAxis("Vertical");

        Vector3 pos = transform.position;
        pos.x += xAxis * speed * Time.deltaTime;
        pos.y += yAxis * speed * Time.deltaTime;
        transform.position = pos;

        transform.rotation = Quaternion.Euler(yAxis * pitchMult, xAxis * rollMult, 0);

        deltaTimeFire += Time.deltaTime;

        /*if (Input.GetKey(KeyCode.Space) && deltaTimeFire >= fireRate)
        {
            TempFire();
            deltaTimeFire = 0f;
        }*/

        if(Input.GetAxis("Jump") == 1 && fireDelegate != null)
        {
            fireDelegate();
        }
    }

    void TempFire()
    {
        GameObject projGO = Instantiate<GameObject>(projectilePrefab);
        projGO.transform.position = transform.position;
        Rigidbody rigidB = projGO.GetComponent<Rigidbody>();
        //rigidB.velocity = Vector3.up * projectileSpeed;

        Projectile proj = projGO.GetComponent<Projectile>();
        proj.type = WeaponType.blaster;
        float tSpeed = Main.GetWeaponDefition(proj.type).velocity;
        rigidB.velocity = Vector3.up * tSpeed;
    }

    private void OnTriggerEnter(Collider other)
    {
        Transform rootT = other.gameObject.transform.root;
        GameObject go = rootT.gameObject;
        //print(other.gameObject.name);
        //print("triggered: " + go.name);

        if(go == lastTriggerGo)
        {
            return;
        }
        lastTriggerGo = go;
        if(go.tag == "Enemy")
        {
            
            shieldLevel--;
            if (!audioSource.isPlaying)
            {
                audioSource.PlayOneShot(loseSounds[Random.Range(0, loseSounds.Length)].clip);
            }
            Destroy(go);

        }
        else if(go.tag == "PowerUp")
        {
            AbsorbPowerUp(go);
            if (!audioSource.isPlaying)
            {
                audioSource.PlayOneShot(powerUpSounds[Random.Range(0,powerUpSounds.Length)].clip);
            }
            
        }
        else
        {
            print("Triggered by non-Enemy:" + go.name);
        }
    }

    public void AbsorbPowerUp(GameObject go)
    {
        PowerUp pu = go.GetComponent<PowerUp>();
        switch (pu.type)
        {
            case WeaponType.shield:
                shieldLevel++;
                break;
            default:
                if(pu.type == weapons[0].type)
                {
                    Weapon w = GetEmptyWeaponSlot();
                    if(w != null)
                    {
                        w.SetType(pu.type);
                    }
                }
                else
                {
                    ClearWeapons();
                    weapons[0].SetType(pu.type);
                }
                break;
        }
        pu.AbsorbedBy(this.gameObject);
    }

    public float shieldLevel
    {
        get
        {
            return (_shieldLevel);
        }
        set
        {
            _shieldLevel = Mathf.Min(value, 4);
            if(value < 0)
            {
                Destroy(this.gameObject);
                Main.S.DelayedRestart(gameRestartDelay);
            }
        }
    }

    Weapon GetEmptyWeaponSlot()
    {
        for(int i = 0; i < weapons.Length; i++)
        {
            if(weapons[i].type == WeaponType.none)
            {
                return weapons[i];
            }
        }
        return null;
    }

    void ClearWeapons()
    {
        foreach(Weapon w in weapons)
        {
            w.SetType(WeaponType.none);
        }
    }
}
