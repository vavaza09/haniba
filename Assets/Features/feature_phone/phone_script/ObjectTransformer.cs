using UnityEngine;

public class ObjectTransformer : MonoBehaviour
{
    [Header("Data")]
    public RideOffer rideOffer;
    [SerializeField] RideDatabase rideDatabase;
    [SerializeField] RideManager rideManager;
    [SerializeField] CamManage camManager;
    // Asset ตั้งต้นใน Inspector
    [Tooltip("Clone the RideOffer asset at runtime so original asset isn't modified.")]
    [SerializeField] bool cloneRideOfferAtRuntime = true;

    [Header("Trigger")]
    [SerializeField] string playerTag = "Player";
    [SerializeField] Transform spanwPoint;
 
    // Refs
    Person person;
    GameObject current;
    SpawnDialogMediator SDM;
    Collider cl;

    // เราจะเก็บ runtime instance ไว้ที่นี่ (ถ้าเปิด cloneRideOfferAtRuntime)
    RideOffer runtimeRideOffer;

    void Awake()
    {
        // หา Mediator อัตโนมัติ
        if (SDM == null)
            SDM = FindFirstObjectByType<SpawnDialogMediator>();
        if (rideManager == null) rideManager = FindFirstObjectByType<RideManager>();
        Debug.Log(rideManager);
        // ถ้าอยากหา rideOffer อัตโนมัติ (เผื่อไม่ได้ลากใน Inspector)
        if (rideOffer == null)
        {
            //rideOffer = GetComponent<RideOffer>();
            //if (rideOffer == null) rideOffer = GetComponentInChildren<RideOffer>();
            //if (rideOffer == null) rideOffer = FindFirstObjectByType<RideOffer>();
            rideOffer = rideDatabase.offers[rideManager.index];
        }
        if(camManager == null) camManager = FindFirstObjectByType<CamManage>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        // เราต้องมี rideOffer ต้นแบบอย่างน้อยสักอัน (เพื่อใช้ prefab / โครงสร้าง)
        if (rideOffer == null)
        {
            Debug.LogWarning("[ObjectTransformer] rideOffer is null. Cannot proceed.");
            return;
        }

        // เตรียม runtime copy (กันไปแก้ asset จริง)
        var activeOffer = GetActiveOffer();

        // ถ้ายังไม่มี prefab จากฝั่งคน ก็ใช้ของเดิมที่อยู่ใน rideOffer template ไปก่อน
        if (activeOffer.transformPrefab == null)
        {
            Debug.LogWarning("[ObjectTransformer] transformPrefab is null on active RideOffer. Using template's prefab if any.");
            activeOffer.transformPrefab = rideOffer.transformPrefab;
        }

        if (activeOffer.transformPrefab == null)
        {
            Debug.LogWarning("[ObjectTransformer] Missing transformPrefab. Cannot instantiate person.");
            return;
        }

        cl = GetComponent<Collider>();

        // สร้าง object คนจาก prefab
        current = Instantiate(activeOffer.transformPrefab, spanwPoint.position, spanwPoint.rotation);
        person = current.GetComponent<Person>();

        if (person == null)
        {
            Debug.LogWarning("[ObjectTransformer] Instantiated prefab has no Person component.");
            return;
        }

        // ====== จุดสำคัญ: ทำให้ RideOffer SO เหมือนกับ SO ของคนที่ accepted ======
        // สมมติว่า Person มี field/properties ชื่อ Data ที่อ้างไปยัง ScriptableObject ของคนนั้น
        // เราจะ copy ฟิลด์ที่ชื่อและชนิด "ตรงกัน" จาก SO ของคน -> มาใส่ใน activeOffer (runtime)
        ApplyPersonSOToRideOffer(person);

        // ทำงานต่อกับระบบ Dialog/Flow
        if (SDM != null)
        {
            camManager.sidePermission = true;
            SDM.NotifyEnterPickup(person);
        }
        else
            Debug.LogWarning("[ObjectTransformer] SpawnDialogMediator not found.");

        if (cl) cl.isTrigger = false;
    }

    // คืนค่า RideOffer ที่จะใช้จริงใน runtime (โคลนถ้าต้องการ)
    RideOffer GetActiveOffer()
    {
        if (!cloneRideOfferAtRuntime)
            return rideOffer; // ใช้ asset เดิม (ระวัง: การแก้จะไปแก้ asset จริง)

        if (runtimeRideOffer == null)
        {
            // สร้าง instance ใหม่ แล้วก็อปจาก template rideOffer ลงมา
            runtimeRideOffer = ScriptableObject.CreateInstance<RideOffer>();
            // คัดลอกฟิลด์ที่ชื่อ/ชนิดตรงกันจาก template -> runtime
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(rideOffer), runtimeRideOffer);
        }
        return runtimeRideOffer;
    }

    // ก็อปฟิลด์จาก SO ของคนลงใน RideOffer runtime
    void ApplyPersonSOToRideOffer(Person p)
    {
        var activeOffer = GetActiveOffer();

        // 1) ถ้ามี SO ของคน (เช่น p.Data) ให้ copy ฟิลด์ที่ชื่อ/ชนิดตรงกันทั้งหมดด้วย JsonUtility
        var personSO = p.GetType().GetField("Data")?.GetValue(p) as ScriptableObject;
        if (personSO != null)
        {
            // โอเวอร์ไรท์เฉพาะฟิลด์ที่ชื่อ/ชนิดตรงกัน (สะดวกและลดการแมพมือ)
            var json = JsonUtility.ToJson(personSO);
            JsonUtility.FromJsonOverwrite(json, activeOffer);
        }
        else
        {
            // กรณีไม่มี SO ของคนให้ดึงจากตัว Component ตรงๆ (ถ้ามีฟิลด์)
            // ปรับ mapping ด้านล่างตามโปรเจกต์จริงของคุณ
            TryMapCommonFieldsFromPersonComponent(p, activeOffer);
        }

        // 2) ฟิลด์ที่ RideOffer ต้องมีแต่ SO คนไม่มี -> เติมเอง (กันค่า null)
        if (activeOffer.transformPrefab == null)
        {
            // ใช้ prefab ของคนแทน ถ้ามี (เช่น p.gameObject model)
            // ถ้าอยากใช้ prefab เดิมจาก template ให้คงไว้ตามที่ GetActiveOffer ทำไว้ก่อนหน้า
            // ในที่นี้จะไม่เปลี่ยน ถ้าคุณต้องการให้เป็น prefab ของคน ให้แก้บรรทัดนี้:
            // activeOffer.transformPrefab = p.gameObject; // (ถ้าอยากใช้ object ปัจจุบันเป็น prefab)
        }
    }

    // เผื่อกรณีไม่มี SO ของคน: map ฟิลด์ยอดนิยมจาก Component -> RideOffer
    void TryMapCommonFieldsFromPersonComponent(Person p, RideOffer dest)
    {
        // ปรับให้ตรงกับโปรเจกต์จริงของคุณ:
        // ตัวอย่าง mapping ยอดนิยม (คอมเมนต์ไว้เผื่อแก้ทีหลัง):
        //
        // dest.passengerName  = p.DisplayName;
        // dest.passengerPic   = p.PortraitSprite;
        // dest.rating         = p.Rating;
        // dest.pickup         = p.PickupName;
        // dest.dropoff        = p.DropoffName;
        // dest.distance       = p.EstimatedDistanceKm;
        //
        // ถ้า person มี ScriptableObject อยู่แล้ว แนะนำใช้เส้นทาง JsonUtility จาก SO จะชัวร์กว่า
    }
}
