using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using Newtonsoft.Json;
using KMHelper;
using System;
using System.Threading;
using UnityEngine;
using Random = UnityEngine.Random;

public class burmaze : MonoBehaviour {
    private static int _moduleIdCounter = 1;
    private bool _isSolved, _lightsOn;
    private int _moduleId;
    public KMAudio newAudio;
    public KMBombInfo info;
    public KMBombModule module;

    public KMSelectable[] departmentButtons;
    public KMSelectable reset;
    public TextMesh keyText, currentDepartmentText;

    Department StartOfMaze, HumanResources, InformationManagement, Marketing, CorporateCompliance, EmployeeBenefits;

    public static Department[] departments;

    private int key, currentDepartment, previousDepartment, goalFrom, goalTo; //0=HR,1=IM,2=M,3=CC,4=EB,5=S

    private int[] connectingLines = new int[] {/*HR*/1,2,1,2,1,2,1,2,3,4,/*IM*/4,2,4,0,3,4,0,2,/*M*/3,4,3,1,4,0,3,0,/*CC*/1,2,1,2,1,0,1,4,/*EB*/1,2,3,2,3,0,3,0}; //42 Entries

    private MonoRandom rnd;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        module.OnActivate += Activate;
    }

    private void Awake() {
        for (var i = 0; i < 5; i++)
        {
            var j = i;
            departmentButtons[i].OnInteract += delegate {
                handleRoomPress(j);
                return false;
            };
        }
        reset.OnInteract += delegate {
            handleReset();
            return false;
        };
    }

    private void handleRoomPress(int room) {//0=HR,1=IM,2=M,3=CC,4=EB,5=S
        newAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, departmentButtons[room].transform);
        if (!_lightsOn || _isSolved) return;

        if (Department.canGoTo(room)) {
            previousDepartment = currentDepartment;
            currentDepartment = room;
            UpdateDisplay();
            UpdatePossibleMoves();
            Debug.LogFormat("[Bureaucratic Maze #{0}] You moved from {1} to {2}!", _moduleId, departments[previousDepartment].getName(), departments[currentDepartment].getName());
        } else {
            module.HandleStrike();
            var possible = string.Join(", ", Department.getCurrentPossibleMoves().Select(d => d.getName()).ToArray());
            Debug.LogFormat("[Bureaucratic Maze #{0}] Strike! The only possible rooms are {2} but you attempted to go to {1}!", _moduleId, departments[room].getName(), possible);
        }
    }

    private void UpdateDisplay() {
        currentDepartmentText.text = departments[currentDepartment].getName();
    }

    private void handleReset() {
        newAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, reset.transform);
        if (!_lightsOn || _isSolved) return;

        currentDepartment = 5;
        previousDepartment = 5;
        Department.setPossibleMoves(new List<Department> { HumanResources });
        UpdateDisplay();
        Debug.LogFormat("[Bureaucratic Maze #{0}] Module reset!", _moduleId);
    }

    private void UpdatePossibleMoves() {
        if (currentDepartment == goalTo && previousDepartment == goalFrom) {
            module.HandlePass();
            _isSolved = true;
            Debug.LogFormat("[Bureaucratic Maze #{0}] Module solved!", _moduleId);
        } else {
                Department.setPossibleMoves(departments[currentDepartment].getRoutes()[departments[previousDepartment]]);
            
        }
    }

    private void Activate() {
        Init();
        _lightsOn = true;
    }

    private void Init() {
        key = Random.Range(2, 10000);
        keyText.text = key.ToString();
        rnd = new MonoRandom(key);

        Debug.LogFormat("[Bureaucratic Maze #{0}] The module key is {1}.", _moduleId, key.ToString());

        StartOfMaze = new Department(5, "Start");
        HumanResources = new Department(0, "HR");
        InformationManagement = new Department(1, "IM");
        Marketing = new Department(2, "M");
        CorporateCompliance = new Department(3, "CC");
        EmployeeBenefits = new Department(4, "EB");

        departments = new Department[] {HumanResources, InformationManagement, Marketing, CorporateCompliance, EmployeeBenefits, StartOfMaze};

        setupKey();

        

        currentDepartment = 5;
        previousDepartment = 5;
        Department.setPossibleMoves(new List<Department>{HumanResources});
        UpdateDisplay();
    }

    private void setupKey() {
        var departmentOptions = new int[] {0, 1, 2, 3, 4};
        departmentOptions.Shuffle(rnd);
        goalFrom = departmentOptions[0]; //Goal from

        var possGoal = new int[] { 0, 1, 2, 3, 4 };
        possGoal.Shuffle(rnd);
        if (possGoal[0] != goalFrom) {
            goalTo = possGoal[0]; //Goal to
        } else {
            goalTo = possGoal[1];
        }

        Debug.LogFormat("[Bureaucratic Maze #{0}] To solve: Go from {1} to {2}.", _moduleId, departments[goalFrom].getName(), departments[goalTo].getName());

        //HR
        var hr = new int[] {1, 2, 3, 4}; //1,2,1,2,1,2,1,2,3,4
        hr.Shuffle(rnd);
        connectingLines[0] = hr[0]; //S
        connectingLines[1] = hr[1]; //S
        connectingLines[2] = hr[0]; //IM
        connectingLines[3] = hr[1]; //IM
        connectingLines[4] = hr[0]; //M
        connectingLines[5] = hr[1]; //M
        connectingLines[6] = hr[0]; //CC
        connectingLines[7] = hr[1]; //CC
        connectingLines[8] = hr[2]; //EB
        connectingLines[9] = hr[3]; //EB

        //IM
        var im = new int[] { 0, 2, 3, 4 }; //4,2,4,0,3,4,0,0
        im.Shuffle(rnd);
        connectingLines[10] = im[3]; //HR
        connectingLines[11] = im[1]; //HR
        connectingLines[12] = im[3]; //CC
        connectingLines[13] = im[0]; //CC
        connectingLines[14] = im[2]; //M
        connectingLines[15] = im[3]; //M
        connectingLines[16] = im[0]; //EB
        connectingLines[17] = im[2]; //EB

        //M
        var m = new int[] { 0, 1, 3, 4 }; //3,4,3,1,4,0,3,0
        m.Shuffle(rnd);
        connectingLines[18] = m[2]; //IM
        connectingLines[19] = m[3]; //IM
        connectingLines[20] = m[2]; //HR
        connectingLines[21] = m[1]; //HR
        connectingLines[22] = m[3]; //CC
        connectingLines[23] = m[0]; //CC
        connectingLines[24] = m[2]; //EB
        connectingLines[25] = m[0]; //EB

        //CC
        var cc = new int[] { 0, 1, 2, 4 }; //1,2,1,2,1,0,1,4
        cc.Shuffle(rnd);
        connectingLines[26] = cc[1]; //HR
        connectingLines[27] = cc[2]; //HR
        connectingLines[28] = cc[1]; //IM
        connectingLines[29] = cc[2]; //IM
        connectingLines[30] = cc[1]; //M
        connectingLines[31] = cc[0]; //M
        connectingLines[32] = cc[1]; //EB
        connectingLines[33] = cc[3]; //EB

        //HB
        var eb = new int[] { 0, 1, 2, 3 }; //1,2,3,2,3,0,3,0
        eb.Shuffle(rnd);
        connectingLines[34] = eb[1]; //CC
        connectingLines[35] = eb[2]; //CC
        connectingLines[36] = eb[3]; //HR
        connectingLines[37] = eb[2]; //HR
        connectingLines[38] = eb[3]; //M
        connectingLines[39] = eb[0]; //M
        connectingLines[40] = eb[3]; //IM
        connectingLines[41] = eb[0]; //IM

        HumanResources.addRoute(1, hr[0], hr[1]);
        HumanResources.addRoute(2, hr[0], hr[1]);
        HumanResources.addRoute(3, hr[0], hr[1]);
        HumanResources.addRoute(4, hr[2], hr[3]);
        HumanResources.addRoute(5, hr[0], hr[1]);

        InformationManagement.addRoute(0, im[3], im[1]);
        InformationManagement.addRoute(2, im[2], im[3]);
        InformationManagement.addRoute(3, im[3], im[0]);
        InformationManagement.addRoute(4, im[0], im[2]);

        Marketing.addRoute(0, m[2], m[1]);
        Marketing.addRoute(1, m[2], m[3]);
        Marketing.addRoute(3, m[3], m[0]);
        Marketing.addRoute(4, m[2], m[0]);

        CorporateCompliance.addRoute(0, cc[1], cc[2]);
        CorporateCompliance.addRoute(1, cc[1], cc[2]);
        CorporateCompliance.addRoute(2, cc[1], cc[0]);
        CorporateCompliance.addRoute(4, cc[1], cc[3]);

        EmployeeBenefits.addRoute(0, eb[3], eb[2]);
        EmployeeBenefits.addRoute(1, eb[3], eb[0]);
        EmployeeBenefits.addRoute(2, eb[3], eb[0]);
        EmployeeBenefits.addRoute(3, eb[1], eb[2]);
    }

}

public class Department{
    //0=HR,1=IM,2=M,3=CC,4=EB,5=S

    private String name;
    private int id;
    private Dictionary<Department, List<Department>> routes;
    private static List<Department> currentPossibleMoves = new List<Department>();

    public Department(int id, String name)
    {
        this.id = id;
        this.name = name;
        routes = new Dictionary<Department, List<Department>>();
    }

    public static void setPossibleMoves(List<Department> departments) {
        currentPossibleMoves = departments;
    }

    public static void setPossibleMoves(int[] departments)
    {
        currentPossibleMoves = createListFromIds(departments);
    }

    public void addRoute(Department from, List<Department> to) {
        routes.Add(from, to);
    }

    public void addRoute(int from, params int[] to) {
        routes.Add(burmaze.departments[from], createListFromIds(to));
    }

    public Dictionary<Department, List<Department>> getRoutes() {
        return this.routes;
    }

    public String getName()
    {
        return this.name;
    }

    public int getId() {
        return this.id;
    }

    public static List<Department> getCurrentPossibleMoves() {
        return currentPossibleMoves;
    }

    public static bool canGoTo(Department destination) {
        return getCurrentPossibleMoves().Contains(destination);
    }

    public static bool canGoTo(int destination)
    {
        return getCurrentPossibleMoves().Contains(burmaze.departments[destination]);
    }

    private static List<Department> createListFromIds(int[] ids) {
        List<Department> list = new List<Department>();
        foreach (int id in ids) {
            list.Add(burmaze.departments[id]);
        }

        return list;
    }

}
public static class Extensions
{

    // Fisher-Yates Shuffle

    public static IList<T> Shuffle<T>(this IList<T> list, MonoRandom rnd)
    {
        int i = list.Count;
        while (i > 1) {
            int index = rnd.Next(i);
            i--;
            T value = list[index];
            list[index] = list[i];
            list[i] = value;
        }
        return list;
    }

}

public class MonoRandom
{
    /// <summary>Initializes a new instance of the <see cref="T:System.Random" /> class, using a time-dependent default seed value.</summary>
    /// <exception cref="T:System.OverflowException">The seed value derived from the system clock is <see cref="F:System.Int32.MinValue" />, which causes an overflow when its absolute value is calculated. </exception>
    public MonoRandom() : this(Environment.TickCount)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="T:System.Random" /> class, using the specified seed value.</summary>
    /// <param name="seed">A number used to calculate a starting value for the pseudo-random number sequence. If a negative number is specified, the absolute value of the number is used. </param>
    /// <exception cref="T:System.OverflowException">
    ///   <paramref name="seed" /> is <see cref="F:System.Int32.MinValue" />, which causes an overflow when its absolute value is calculated. </exception>
    public MonoRandom(int seed)
    {
        Seed = seed;
        var num = 161803398 - Math.Abs(seed);
        _seedArray[55] = num;
        var num2 = 1;
        for (var i = 1; i < 55; i++)
        {
            var num3 = 21 * i % 55;
            _seedArray[num3] = num2;
            num2 = num - num2;
            if (num2 < 0)
            {
                num2 += int.MaxValue;
            }
            num = _seedArray[num3];
        }
        for (var j = 1; j < 5; j++)
        {
            for (var k = 1; k < 56; k++)
            {
                _seedArray[k] -= _seedArray[1 + (k + 30) % 55];
                if (_seedArray[k] < 0)
                {
                    _seedArray[k] += int.MaxValue;
                }
            }
        }
        _inext = 0;
        _inextp = 31;
    }

    /// <summary>Returns a random number between 0.0 and 1.0.</summary>
    /// <returns>A double-precision floating point number greater than or equal to 0.0, and less than 1.0.</returns>
    protected virtual double Sample()
    {
        if (++_inext >= 56)
        {
            _inext = 1;
        }
        if (++_inextp >= 56)
        {
            _inextp = 1;
        }
        var num = _seedArray[_inext] - _seedArray[_inextp];
        if (num < 0)
        {
            num += int.MaxValue;
        }
        _seedArray[_inext] = num;
        return (double)num * 4.6566128752457969E-10;
    }

    public T ShuffleFisherYates<T>(T list) where T : IList
    {
        // Brings an array into random order using the Fisher-Yates shuffle.
        // This is an inplace algorithm, i.e. the input array is modified.
        var i = list.Count;
        while (i > 1)
        {
            var index = Next(0, i);
            i--;
            var value = list[index];
            list[index] = list[i];
            list[i] = value;
        }
        return list;
    }

    /// <summary>Returns a nonnegative random number.</summary>
    /// <returns>A 32-bit signed integer greater than or equal to zero and less than <see cref="F:System.Int32.MaxValue" />.</returns>
    /// <filterpriority>1</filterpriority>
    public virtual int Next()
    {
        return (int)(Sample() * 2147483647.0);
    }

    /// <summary>Returns a nonnegative random number less than the specified maximum.</summary>
    /// <returns>A 32-bit signed integer greater than or equal to zero, and less than <paramref name="maxValue" />; that is, the range of return values ordinarily includes zero but not <paramref name="maxValue" />. However, if <paramref name="maxValue" /> equals zero, <paramref name="maxValue" /> is returned.</returns>
    /// <param name="maxValue">The exclusive upper bound of the random number to be generated. <paramref name="maxValue" /> must be greater than or equal to zero. </param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    ///   <paramref name="maxValue" /> is less than zero. </exception>
    /// <filterpriority>1</filterpriority>
    public virtual int Next(int maxValue)
    {
        if (maxValue < 0)
        {
            throw new ArgumentOutOfRangeException("maxValue");
        }
        return (int)(Sample() * (double)maxValue);
    }

    /// <summary>Returns a random number within a specified range.</summary>
    /// <returns>A 32-bit signed integer greater than or equal to <paramref name="minValue" /> and less than <paramref name="maxValue" />; that is, the range of return values includes <paramref name="minValue" /> but not <paramref name="maxValue" />. If <paramref name="minValue" /> equals <paramref name="maxValue" />, <paramref name="minValue" /> is returned.</returns>
    /// <param name="minValue">The inclusive lower bound of the random number returned. </param>
    /// <param name="maxValue">The exclusive upper bound of the random number returned. <paramref name="maxValue" /> must be greater than or equal to <paramref name="minValue" />. </param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    ///   <paramref name="minValue" /> is greater than <paramref name="maxValue" />. </exception>
    /// <filterpriority>1</filterpriority>
    public virtual int Next(int minValue, int maxValue)
    {
        if (minValue > maxValue)
        {
            throw new ArgumentOutOfRangeException("minValue");
        }
        var num = (uint)(maxValue - minValue);
        if (num <= 1u)
        {
            return minValue;
        }
        return (int)((ulong)((uint)(Sample() * num)) + (ulong)((long)minValue));
    }

    /// <summary>Fills the elements of a specified array of bytes with random numbers.</summary>
    /// <param name="buffer">An array of bytes to contain random numbers. </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///   <paramref name="buffer" /> is null. </exception>
    /// <filterpriority>1</filterpriority>
    public virtual void NextBytes(byte[] buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = (byte)(Sample() * 256.0);
        }
    }

    /// <summary>Returns a random number between 0.0 and 1.0.</summary>
    /// <returns>A double-precision floating point number greater than or equal to 0.0, and less than 1.0.</returns>
    /// <filterpriority>1</filterpriority>
    public virtual double NextDouble()
    {
        return Sample();
    }

    public int Seed { get; private set; }

    private int _inext;
    private int _inextp;
    private readonly int[] _seedArray = new int[56];
}
