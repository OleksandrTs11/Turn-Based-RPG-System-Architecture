using System.Collections;
using UnityEngine;

public partial class BattleScript : MonoBehaviour
{
    [Header("Warrior properties")]
    public GameObject WarriorObject;
    public Transform warriorTargetPos;
    private UnityEngine.AI.NavMeshAgent warriorAgent;
    private Animator warriorAnimator;

    [Header("Mage properties")]
    public GameObject MageObject;
    public Transform mageTargetPos;
    private UnityEngine.AI.NavMeshAgent mageAgent;
    private Animator mageAnimator;

    [Header("Battle properties")]
    public string FirstTurn = " ";
    private Warrior warrior;
    private Mage mage;

    private Vector3 warriorHome;
    private Vector3 mageHome;

    public void Start()
    {
        // Инициализация персонажей (Данные)
        warrior = new Warrior("Ivan", 400, 400, 20, 3, 10, 80);
        mage = new Mage("Gandalf", 250, 250, 40, 3, 30, 100, 100);

        // Сохраняем позиции "дома"
        warriorHome = WarriorObject.transform.position;
        mageHome = MageObject.transform.position;

        // Кэшируем компоненты Unity
        warriorAgent = WarriorObject.GetComponent<UnityEngine.AI.NavMeshAgent>();
        warriorAnimator = WarriorObject.GetComponent<Animator>();

        mageAgent = MageObject.GetComponent<UnityEngine.AI.NavMeshAgent>();
        mageAnimator = MageObject.GetComponent<Animator>();

        Debug.Log("<color=white>Битва начинается!</color>");
        StartCoroutine(BattleTurn());
    }

    // Универсальный метод перемещения: Идет к цели -> Бьет -> Возвращается
    IEnumerator MoveAndAttack(UnityEngine.AI.NavMeshAgent agent, Animator anim, Vector3 destination, Vector3 homePosition, System.Action attackLogic)
    {
        // 1. Идем к врагу
        agent.isStopped = false;
        agent.SetDestination(destination);
        anim.SetBool("isWalking", true);

        yield return new WaitForSeconds(0.1f);

        while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
        {
            yield return null;
        }

        // 2. Атакуем
        anim.SetBool("isWalking", false);

        // Поворачиваемся к врагу перед ударом (игнорируя высоту Y)
        Vector3 lookAtEnemy = new Vector3(destination.x, agent.transform.position.y, destination.z);
        agent.transform.LookAt(lookAtEnemy);

        anim.SetTrigger("Attack");
        attackLogic.Invoke();

        // Ждем завершения анимации удара
        yield return new WaitForSeconds(1.5f);

        // --- 3. ТЕЛЕПОРТАЦИЯ ДОМОЙ ---
        agent.enabled = false; // Отключаем агент, чтобы он не мешал телепортации
        agent.transform.position = homePosition; // Мгновенно перемещаем
        agent.enabled = true; // Включаем обратно

        // 4. ПРАВИЛЬНЫЙ ПОВОРОТ ПОСЛЕ ТЕЛЕПОРТАЦИИ
        // Разворачиваем персонажа лицом к цели (магу)
        Vector3 lookAtTarget = new Vector3(destination.x, agent.transform.position.y, destination.z);
        agent.transform.LookAt(lookAtTarget);

        anim.SetBool("isWalking", false);
        Debug.Log("Вернулся домой и развернулся.");
    }

    IEnumerator BattleTurn()
    {
        yield return new WaitForSeconds(1f);
        int coinFlip = UnityEngine.Random.Range(1, 3);
        FirstTurn = (coinFlip == 1) ? "Warrior" : "Mage";
        Debug.Log($"<color=yellow>Первым ходит: {FirstTurn}</color>");

        while (warrior.Hp > 0 && mage.Hp > 0)
        {
            if (FirstTurn == "Warrior")
            {
                yield return StartCoroutine(WarriorTurn());
                if (mage.Hp <= 0) break;
                yield return new WaitForSeconds(1f);
                yield return StartCoroutine(MageTurn());
            }
            else
            {
                yield return StartCoroutine(MageTurn());
                if (warrior.Hp <= 0) break;
                yield return new WaitForSeconds(1f);
                yield return StartCoroutine(WarriorTurn());
            }
            yield return new WaitForSeconds(1f);
            Debug.Log("--- Следующий раунд ---");
        }
        Debug.Log("<color=gold>КОНЕЦ БОЯ</color>");
    }

    IEnumerator WarriorTurn()
    {
        Debug.Log("<color=orange>Ход ВОИНА: 1-Атака, 2-PowerStrike, 3-Heal</color>");
        bool actionMade = false;

        while (!actionMade)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                yield return StartCoroutine(MoveAndAttack(warriorAgent, warriorAnimator, mageTargetPos.position, warriorHome, () =>
                {
                    warrior.Attack(mage, 25, 15);
                }));
                actionMade = true;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                yield return StartCoroutine(MoveAndAttack(warriorAgent, warriorAnimator, mageTargetPos.position, warriorHome, () =>
                {
                    warrior.PowerStrike(mage);
                }));
                actionMade = true;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                warrior.Heal();
                actionMade = true;
            }
            yield return null;
        }
    }

    IEnumerator MageTurn()
    {
        Debug.Log("<color=cyan>Ход МАГА: 1-Атака, 2-CastSpell, 3-Зелья</color>");
        bool actionMade = false;

        while (!actionMade)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                // Маг атакует с места (дистанционно)
                mage.Attack(warrior, 15);
                actionMade = true;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                mage.CastSpell(warrior);
                actionMade = true;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                Debug.Log("Выбери: 1-Хил, 2-Мана");
                bool potionUsed = false;
                while (!potionUsed)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha1)) { mage.Heal(); potionUsed = true; actionMade = true; }
                    else if (Input.GetKeyDown(KeyCode.Alpha2)) { mage.manaRegen(); potionUsed = true; actionMade = true; }
                    yield return null;
                }
            }
            yield return null;
        }
    }

    // --- КЛАССЫ ПЕРСОНАЖЕЙ (ЛОГИКА) ---

    public class Character
    {
        public string Name { get; private set; }
        public int Hp { get; protected set; }
        public int MaxHp { get; private set; }
        public int Dmg { get; protected set; }
        public int Agility { get; private set; }
        public int PotionsCount { get; protected set; }

        public Character(string name, int hp, int maxhp, int dmg, int potions, int agility)
        {
            Name = name; Hp = hp; Dmg = dmg; PotionsCount = potions; MaxHp = maxhp; Agility = agility;
        }

        public void TakeDamage(int damage)
        {
            Hp -= damage;
            if (Hp < 0) Hp = 0;
            Debug.Log($"<color=red>{Name} получает {damage} урона. Оставшееся HP: {Hp}</color>");
        }

        public virtual void Attack(Character target, int currentDamage, int CritChance = 0, bool isSpecial = false)
        {
            if (UnityEngine.Random.Range(1, 101) <= target.Agility)
            {
                Debug.Log($"<color=cyan>{target.Name} уклонился!</color>");
                return;
            }
            target.TakeDamage(currentDamage);
        }

        public virtual void Heal()
        {
            if (PotionsCount > 0)
            {
                int healAmount = 30;
                Hp = Mathf.Min(Hp + healAmount, MaxHp);
                PotionsCount--;
                Debug.Log($"<color=green>{Name} исцелен до {Hp}. Зелий осталось: {PotionsCount}</color>");
            }
            else Debug.Log("Зелья закончились!");
        }
    }

    public class Warrior : Character
    {
        public int stamina;
        public Warrior(string name, int hp, int maxhp, int dmg, int potions, int agility, int stamina)
            : base(name, hp, maxhp, dmg, potions, agility) { this.stamina = stamina; }

        public override void Attack(Character target, int currentDamage, int CritChance = 0, bool isSpecial = false)
        {
            int finalDmg = currentDamage;
            if (CritChance > 0 && UnityEngine.Random.Range(1, 101) <= CritChance)
            {
                finalDmg *= 3;
                Debug.Log($"<color=red>{Name} КРИТАНУЛ!</color>");
            }

            if (!isSpecial) { stamina += 20; }
            base.Attack(target, finalDmg, CritChance, isSpecial);
        }

        public void PowerStrike(Character target)
        {
            if (stamina >= 40)
            {
                Attack(target, Dmg * 2, 20, true);
                stamina -= 40;
                Debug.Log($"<color=orange>{Name} использовал Power Strike! Энергия: {stamina}</color>");
            }
            else Debug.Log("Недостаточно энергии!");
        }
    }

    public class Mage : Character
    {
        public int Mana { get; private set; }
        public int MaxMana { get; private set; }
        public Mage(string name, int hp, int maxhp, int dmg, int potions, int agility, int mana, int maxMana)
            : base(name, hp, maxhp, dmg, potions, agility) { Mana = mana; MaxMana = maxMana; }

        public void CastSpell(Character target)
        {
            if (Mana >= 30)
            {
                target.TakeDamage(40);
                Mana -= 30;
                Debug.Log($"<color=blue>{Name} пустил огненный шар! Мана: {Mana}</color>");
            }
            else Debug.Log("Мана закончилась!");
        }

        public void manaRegen()
        {
            if (PotionsCount > 0)
            {
                Mana = Mathf.Min(Mana + 30, MaxMana);
                PotionsCount--;
                Debug.Log($"<color=blue>Мана восстановлена: {Mana}</color>");
            }
        }

        public override void Heal()
        {
            base.Heal();
            Mana = Mathf.Min(Mana + 10, MaxMana);
        }
    }
}