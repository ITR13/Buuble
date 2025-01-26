using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Game : MonoBehaviour
{
    [Serializable]
    private struct Upgrade
    {
        public int CountToChange;
        public GameObject Prerequisite;
        public GameObject Prefab;
    }


    private const int StateIndex = 0;
    private const int LevelIndex = 1;
    private const int BubbleIndex = 2;
    private const int EnemyIndex = 3;
    private const int BulletParent = 4;

    private const float MinSize = 0.75f;

    [SerializeField] private GameObject _bubblePrefab, _shooterPrefab;
    [SerializeField] private GameObject[] _enemyPrefabs;
    [SerializeField] private Camera _mainCamera;

    [SerializeField] private GameObject _canvas;
    [SerializeField] private Button[] _choices;
    [SerializeField] private Image _countdown;

    [SerializeField] private Upgrade[] _upgrades;

    private GameObject[] _pool;

    private float _timer;

    private float _cameraDistance;
    private float _cameraVelocity;


    private int _timescale = 1;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _timescale = 1;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            _timescale = 2;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            _timescale = 4;
        }
    }

    private void FixedUpdate()
    {
        if (transform.childCount == StateIndex)
        {
            new GameObject("Init").transform.parent = transform;
            _timer = 0;
        }

        switch (transform.GetChild(StateIndex).name)
        {
            case "Init":
                _cameraDistance = -10;
                _canvas.SetActive(false);
                Init();
                break;
            case "StartRound":
                Time.timeScale *= 1.001f;
                _cameraDistance = -60;
                _canvas.SetActive(false);
                StartRound();
                UpdateBubbles(false);
                break;
            case "PlayRound":
                Time.timeScale = _timescale;
                _cameraDistance = -60;
                _canvas.SetActive(false);
                PlayRound();
                UpdateBubbles(true);
                break;
            case "RoundWin":
                Time.timeScale *= 1.001f;
                _cameraDistance = -10;
                _canvas.SetActive(false);
                RoundWin();
                UpdateBubbles(false);
                break;
            case "Shop":
                _cameraDistance = -10;
                _countdown.fillAmount = (30 - _timer) / 30;
                _canvas.SetActive(true);
                UpdateBubbles(false);

                _timer += Time.deltaTime;
                if (_timer >= 31)
                {
                    var upgrades = GetValidUpgrades();
                    if (upgrades.Count > 0)
                    {
                        var upgrade = upgrades[0];
                        var toConvert = upgrade.CountToChange;
                        for (var index = 0; index < _pool.Length && toConvert > 0; index++)
                        {
                            if (_pool[index] != upgrade.Prerequisite) continue;
                            _pool[index] = upgrade.Prefab;
                            toConvert--;
                        }
                    }

                    SetState("StartRound");
                }

                break;
            default:
                SetState("Init");
                return;
        }

        var newDistance = Mathf.SmoothDamp(_mainCamera.transform.position.z, _cameraDistance, ref _cameraVelocity, 3f);
        _mainCamera.transform.position = new Vector3(0, 0, newDistance);
    }

    private void SetState(string state)
    {
        Time.timeScale = 1;
        transform.GetChild(StateIndex).name = state;
        _timer = 0;
    }

    private void Init()
    {
        foreach (var c in GetComponentsInChildren<OnDestroyScript>())
        {
            Destroy(c);
        }

        for (var i = transform.childCount - 1; i > 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
            Destroy(child);
        }

        _pool = new GameObject[100];
        for (var i = 0; i < _pool.Length; i++)
        {
            _pool[i] = i < 10 ? _shooterPrefab : _bubblePrefab;
        }

        _mainCamera.transform.position = new Vector3(0, 0, _cameraDistance);

        new GameObject("0").transform.parent = transform;
        new GameObject("BubbleParent").transform.parent = transform;
        new GameObject("EnemyParent").transform.parent = transform;
        new GameObject("BulletParent").transform.parent = transform;
        SetState("StartRound");
    }

    private void StartRound()
    {
        _timer += Time.deltaTime;
        if (_timer < 0.05f) return;
        _timer -= 0.05f;

        var bubbleParent = transform.GetChild(BubbleIndex);
        if (bubbleParent.childCount >= 110)
        {
            SetState("PlayRound");
            return;
        }

        var level = int.Parse(transform.GetChild(LevelIndex).name) - 1;

        var startIndex = level / 5;
        var baseAmount = (level % 5) * 0.2f + 0.2f;
        // ReSharper disable once PossibleLossOfFraction
        var secondAmount = ((level % 5) / 2) * 0.2f;

        startIndex = Mathf.Min(startIndex, _enemyPrefabs.Length - 1);

        GameObject enemyPrefab = null;

        if (level < 0)
        {
            if (bubbleParent.childCount % 3 == 0 && bubbleParent.childCount < 4 * 3)
            {
                enemyPrefab = _enemyPrefabs[0];
            }
        }
        else if (level % 5 == 4 && startIndex < _enemyPrefabs.Length - 2 && bubbleParent.childCount == 100)
        {
            enemyPrefab = _enemyPrefabs[startIndex + 2];
        }
        else if (startIndex < _enemyPrefabs.Length - 1 && Random.Range(0f, 1f) <= secondAmount)
        {
            enemyPrefab = _enemyPrefabs[startIndex + 1];
        }
        else if (bubbleParent.childCount == 1 || Random.Range(0f, 1f) <= baseAmount)
        {
            enemyPrefab = _enemyPrefabs[startIndex];
        }
        else if (level >= 5)
        {
            enemyPrefab = _enemyPrefabs[startIndex - 1];
        }

        if (enemyPrefab != null)
        {
            var enemyParent = transform.GetChild(EnemyIndex);
            var enemy = Instantiate(enemyPrefab, enemyParent);
            enemy.name = enemyPrefab.name;
            enemy.layer = LayerMask.NameToLayer("Enemy");
            enemy.transform.position = Random.insideUnitCircle.normalized * (30 + bubbleParent.childCount / 5f);
        }

        FindAnyObjectByType<AudioPlayer>().PlayAudio(true);
        var prefab = _pool[Random.Range(0, _pool.Length)];
        var child = Instantiate(prefab, bubbleParent);
        child.name = prefab.name;
        var randomOffset = Random.insideUnitCircle / 5;
        child.transform.position = randomOffset;
        child.layer = LayerMask.NameToLayer("Bubble");
    }

    private void PlayRound()
    {
        var bubbleParent = transform.GetChild(BubbleIndex);
        var enemyParent = transform.GetChild(EnemyIndex);
        var bulletParent = transform.GetChild(BulletParent);

        if (bubbleParent.childCount == 0)
        {
            SetState("GameOver");
            return;
        }

        if (enemyParent.childCount == 0)
        {
            SetState("RoundWin");
            return;
        }

        var inRange = new List<Transform>();
        foreach (Transform enemy in enemyParent)
        {
            var distance = enemy.position.sqrMagnitude;
            if (distance > 30 * 30) continue;
            inRange.Add(enemy);
        }

        if (inRange.Count == 0) return;

        foreach (Transform bubble in bubbleParent)
        {
            float range, fireRate, speed, bulletCount;
            switch (bubble.gameObject.name)
            {
                case "Shooter":
                    range = 10;
                    fireRate = 0.25f;
                    speed = 5;
                    bulletCount = 1;
                    break;
                case "RapidShooter":
                    range = 10;
                    fireRate = 0.5f;
                    speed = 5;
                    bulletCount = 1;
                    break;
                case "RapiderShooter":
                    range = 10;
                    fireRate = 1f;
                    speed = 5;
                    bulletCount = 1;
                    break;
                case "RapidestShooter":
                    range = 10;
                    fireRate = 2f;
                    speed = 5;
                    bulletCount = 1;
                    break;
                case "DoubleShooter":
                    range = 10;
                    fireRate = 0.5f;
                    speed = 5;
                    bulletCount = 2;
                    break;
                case "QuintupleShooter":
                    range = 10;
                    fireRate = 0.5f;
                    speed = 5;
                    bulletCount = 5;
                    break;
                case "Sniper":
                    range = 20;
                    fireRate = 0.1f;
                    speed = 12;
                    bulletCount = 1;
                    break;
                case "Sniperer":
                    range = 20;
                    fireRate = 0.1f;
                    speed = 12;
                    bulletCount = 3;
                    break;
                case "Sniperest":
                    range = 20;
                    fireRate = 0.1f;
                    speed = 12;
                    bulletCount = 5;
                    break;
                default:
                    continue;
            }

            if (bubble.localScale.x < 1)
            {
                var unlerp = Mathf.InverseLerp(MinSize, 1, bubble.localScale.x);
                unlerp += Time.deltaTime * fireRate;
                var next = Mathf.Clamp01(Mathf.Lerp(MinSize, 1, unlerp));
                bubble.localScale = new Vector3(next, next, next);
                continue;
            }

            var closestIndex = -1;
            var closestDistance = Mathf.Infinity;

            for (var index = 0; index < inRange.Count; index++)
            {
                var enemy = inRange[index];
                var delta = enemy.position - bubble.position;
                var dist = delta.sqrMagnitude;

                if (dist > range * range) continue;
                if (dist > closestDistance) continue;
                closestDistance = dist;
                closestIndex = index;
            }

            if (closestIndex < 0) continue;

            bubble.localScale = new Vector3(MinSize, MinSize, MinSize);
            var direction = (inRange[closestIndex].position - bubble.position).normalized;
            var bulletPosition = bubble.transform.position + direction / 2;

            var maxAngle = 5;
            if (bubble.gameObject.name.Contains("Sniper"))
            {
                maxAngle = 0;
            }

            for (var bulletIndex = 0; bulletIndex < bulletCount; bulletIndex++)
            {
                var randomAngle = Random.Range(-maxAngle, maxAngle);
                var rotation = Quaternion.Euler(0, 0, randomAngle);

                var child = Instantiate(bubble.GetChild(0), bulletParent);
                child.transform.position = bulletPosition;
                child.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                child.name = bubble.name;
                child.gameObject.AddComponent<Rigidbody2D>().linearVelocity = rotation * direction * speed;
                child.gameObject.AddComponent<CircleCollider2D>().isTrigger = true;
                child.gameObject.AddComponent<BulletScript>();
                child.gameObject.layer = LayerMask.NameToLayer("Bullet");
                inRange.RemoveAt(closestIndex);


                Destroy(child.gameObject, 10f);
                break;
            }
        }
    }

    private void RoundWin()
    {
        _timer += Time.deltaTime;
        if (_timer < 0.05f) return;
        _timer -= 0.05f;

        var bubbleParent = transform.GetChild(BubbleIndex);
        if (bubbleParent.childCount > 0)
        {
            Destroy(bubbleParent.GetChild(0).gameObject);
            return;
        }

        var level = transform.GetChild(LevelIndex);
        level.name = (int.Parse(level.name) + 1).ToString();

        var upgrades = GetValidUpgrades();
        for (var i = 0; i < _choices.Length; i++)
        {
            var choice = _choices[i];
            if (i >= upgrades.Count)
            {
                choice.gameObject.SetActive(false);
                continue;
            }

            var upgrade = upgrades[i];
            choice.gameObject.SetActive(true);
            var text = choice.GetComponentInChildren<TextMeshProUGUI>();
            var name1 = Regex.Replace(upgrade.Prerequisite.name, @"\d|\s", "");
            var name2 = Regex.Replace(upgrade.Prefab.name, @"\d|\s", "");

            text.text = $"Convert\n<b>{upgrade.CountToChange}% {name1}</b>\nto\n<b>{upgrade.CountToChange}% {name2}</b>";

            choice.onClick.RemoveAllListeners();
            choice.onClick.AddListener(
                () =>
                {
                    if (!_canvas.activeSelf) return;
                    _canvas.SetActive(false);
                    var toConvert = upgrade.CountToChange;
                    for (var index = 0; index < _pool.Length && toConvert > 0; index++)
                    {
                        if (_pool[index] != upgrade.Prerequisite) continue;
                        _pool[index] = upgrade.Prefab;
                        toConvert--;
                    }

                    SetState("StartRound");
                }
            );
        }

        if (upgrades.Count == 0)
        {
            SetState("StartRound");
        }
        else
        {
            SetState("Shop");
        }
    }

    private void UpdateBubbles(bool updateEnemies)
    {
        var bubbleParent = transform.GetChild(BubbleIndex);
        var enemyParent = transform.GetChild(EnemyIndex);

        var bubbleCount = bubbleParent.childCount;

        var transforms = new Transform[bubbleCount + enemyParent.childCount];
        var rigidbodies = new Rigidbody2D[transforms.Length];

        var positions = new Vector2[transforms.Length];
        var force = new Vector2[transforms.Length];

        var destroyed = new List<GameObject>();

        for (var i = 0; i < bubbleCount; i++)
        {
            transforms[i] = bubbleParent.GetChild(i);
            rigidbodies[i] = transforms[i].gameObject.GetComponent<Rigidbody2D>();
            positions[i] = transforms[i].position;
        }

        for (var i = bubbleCount; i < transforms.Length; i++)
        {
            transforms[i] = enemyParent.GetChild(i - bubbleCount);
            rigidbodies[i] = transforms[i].gameObject.GetComponent<Rigidbody2D>();
            positions[i] = transforms[i].position;
        }

        for (var i = 0; i < positions.Length; i++)
        {
            if (i >= bubbleCount && !updateEnemies) break;
            var centerForce = i < bubbleCount ? 1 : 4;

            var distFromZero = Vector2.Distance(positions[i], Vector2.zero) / 3;
            if (distFromZero > 20)
            {
                centerForce *= 20;
            }


            force[i] -= positions[i].normalized * Mathf.Lerp(0, centerForce, distFromZero);
            for (var j = 1; j < positions.Length; j++)
            {
                var delta = (positions[i] - positions[j]);
                var sqrDistance = delta.sqrMagnitude;
                if (sqrDistance < 1)
                {
                    if (i < bubbleCount && j >= bubbleCount)
                    {
                        destroyed.Add(transforms[i].gameObject);
                        if (transforms[i].gameObject.name.Contains("Spike"))
                        {
                            destroyed.Add(transforms[j].gameObject);
                        }

                        positions[j] = Random.insideUnitCircle.normalized * 30;
                        transforms[j].position = positions[j];
                        break;
                    }

                    var dirForce = delta.normalized * Mathf.Lerp(10, 0, sqrDistance);
                    force[i] += dirForce;
                    force[j] -= dirForce;
                }
                else if (sqrDistance < 3)
                {
                    sqrDistance -= 2;
                    sqrDistance *= sqrDistance;
                    sqrDistance = 1 - sqrDistance;
                    var dirForce = delta.normalized * Mathf.Clamp01(sqrDistance);
                    force[i] -= dirForce;
                    force[j] += dirForce;
                }
            }
        }


        for (var i = 0; i < rigidbodies.Length; i++)
        {
            rigidbodies[i].AddForce(force[i]);
        }

        foreach (var go in destroyed) GameObject.Destroy(go);
    }

    private List<Upgrade> GetValidUpgrades()
    {
        var counts = new Dictionary<GameObject, int>();
        foreach (var bubble in _pool)
        {
            counts.TryAdd(bubble, 0);
            counts[bubble]++;
        }

        var upgrades = new List<Upgrade>();
        for (var i = 0; i < _upgrades.Length; i++)
        {
            var upgrade = _upgrades[i];
            counts.TryGetValue(upgrade.Prerequisite, out var count);
            if (upgrade.Prerequisite == _bubblePrefab && count == 0 && upgrade.Prefab != _shooterPrefab)
            {
                counts.TryGetValue(_shooterPrefab, out count);
                upgrade.Prerequisite = _shooterPrefab;
            }

            if (count < upgrade.CountToChange) continue;
            upgrades.Add((upgrade));
        }

        Shuffle(upgrades);
        return upgrades;
    }

    private void Shuffle(List<Upgrade> upgrades)
    {
        for (var i = upgrades.Count - 1; i > 0; i--)
        {
            var r = Random.Range(0, i);
            (upgrades[r], upgrades[i]) = (upgrades[i], upgrades[r]);
        }
    }
}