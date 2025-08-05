using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct BitSet
{
    public int Value;
    public BitSet(int value) => Value = value;

    public bool HasBit(int position) => (Value & (1 << position)) != 0;
    public void SetBit(int position) => Value |= (1 << position);
    public void ClearBit(int position) => Value &= ~(1 << position);
    public bool IsEmpty() => Value == 0;

    public int PopCount()
    {
        int v = Value;
        int c = 0;

        while (v != 0)
        {
            c += v & 1;
            v >>= 1;
        }
        return c;
    }

    public static BitSet And(BitSet a, BitSet b) => new BitSet(a.Value & b.Value);
    public static BitSet Or(BitSet a, BitSet b) => new BitSet(a.Value | b.Value);
}

[Serializable]
public struct PairConstraint
{
    public int Pos1;
    public int Pos2;

    public PairConstraint(int pos1, int pos2)
    {
        Pos1 = pos1;
        Pos2 = pos2;
    }
}

public class CSPSolver
{
    private BitSet[] _domains;
    private readonly List<PairConstraint> _requiredPairs = new List<PairConstraint>();
    private readonly int[] _valueCounts = new int[10];

    private int[][] _neighborCache;
    private readonly int[] _neighborOffsets = {
        -9 - 1, -9, -9 + 1,  // Top row
        -1,          +1,      // Same row
        +9 - 1, +9, +9 + 1   // Bottom row
    };

    private readonly Stack<BitSet[]> _domainStack = new Stack<BitSet[]>();
    private readonly Queue<int[]> _arrayPool = new Queue<int[]>();

    private const int MAX_RECURSION_DEPTH = 1000;
    private const int YIELD_FREQUENCY = 10;
    private const int MAX_PAIR_ATTEMPTS = 50;

    private readonly int _columns;
    private readonly int _activeTileCount;

    public CSPSolver(int columns, int activeTileCount)
    {
        _columns = columns;
        _activeTileCount = activeTileCount;
    }

    public void Initialize(int activeTileCount)
    {
        _domains = new BitSet[activeTileCount];
        _requiredPairs.Clear();
        _domainStack.Clear();
        Array.Clear(_valueCounts, 0, _valueCounts.Length);

        // Initialize domains with all values 1-9
        for (int i = 0; i < activeTileCount; i++)
        {
            _domains[i] = new BitSet(0b1111111110); // Bits 1-9 set
        }
    }

    public void PreCalculateNeighbors(int activeTileCount)
    {
        _neighborCache = new int[activeTileCount][];

        for (int i = 0; i < activeTileCount; i++)
        {
            var neighbors = new List<int>(8);

            foreach (int offset in _neighborOffsets)
            {
                var neighborIndex = i + offset;

                // Bounds checking
                if (neighborIndex >= 0 && neighborIndex < activeTileCount)
                {
                    int currentRow = i / _columns;
                    int neighborRow = neighborIndex / _columns;

                    if (Math.Abs(currentRow - neighborRow) <= 1)
                    {
                        neighbors.Add(neighborIndex);
                    }
                }
            }

            _neighborCache[i] = neighbors.ToArray();
        }
    }

    public void GenerateRequiredPairs(int stage)
    {
        var pairCount = GetPairCount(stage);
        var usedPositions = new BitSet(0);
        var posStillEmpty = new List<int>(_activeTileCount);

        for (int i = 0; i < pairCount; i++)
        {
            posStillEmpty.Clear();
            for (int j = 0; j < _activeTileCount; j++)
            {
                if (!usedPositions.HasBit(j))
                    posStillEmpty.Add(j);
            }

            if (posStillEmpty.Count < 2) break;

            var foundPair = false;
            for (int attempts = 0; attempts < MAX_PAIR_ATTEMPTS && !foundPair; attempts++)
            {
                var pos1 = posStillEmpty[UnityEngine.Random.Range(0, posStillEmpty.Count)];

                // Find available neighbors
                var availableNeighbors = new List<int>();
                foreach (int neighbor in _neighborCache[pos1])
                {
                    if (!usedPositions.HasBit(neighbor))
                        availableNeighbors.Add(neighbor);
                }

                if (availableNeighbors.Count > 0)
                {
                    var pos2 = availableNeighbors[UnityEngine.Random.Range(0, availableNeighbors.Count)];

                    _requiredPairs.Add(new PairConstraint(pos1, pos2));
                    usedPositions.SetBit(pos1);
                    usedPositions.SetBit(pos2);
                    foundPair = true;
                }
                else
                {
                    posStillEmpty.Remove(pos1);
                }
            }
        }
    }

    public void ApplyInitialConstraints()
    {
        var validPairsBits = new BitSet[10];

        for (int val = 1; val <= 9; val++)
        {
            validPairsBits[val] = new BitSet(1 << val);
        }

        // Sum to 10 pairs
        validPairsBits[1].SetBit(9); validPairsBits[9].SetBit(1);
        validPairsBits[2].SetBit(8); validPairsBits[8].SetBit(2);
        validPairsBits[3].SetBit(7); validPairsBits[7].SetBit(3);
        validPairsBits[4].SetBit(6); validPairsBits[6].SetBit(4);

        // Apply constraints to required pairs
        foreach (var pair in _requiredPairs)
        {
            var newDomain1 = new BitSet(0);
            var newDomain2 = new BitSet(0);

            for (int v1 = 1; v1 <= 9; v1++)
            {
                if (_domains[pair.Pos1].HasBit(v1))
                {
                    var validV2s = validPairsBits[v1];
                    var intersection = BitSet.And(_domains[pair.Pos2], validV2s);

                    if (!intersection.IsEmpty())
                    {
                        newDomain1.SetBit(v1);
                        newDomain2 = BitSet.Or(newDomain2, intersection);
                    }
                }
            }

            _domains[pair.Pos1] = newDomain1;
            _domains[pair.Pos2] = newDomain2;
        }
    }

    public IEnumerator SolveCSP(Action<bool, int[]> callback)
    {
        var assignment = GetPooledArray();
        Array.Fill(assignment, -1);

        var result = false;
        yield return SolveCSPCoroutine(BacktrackCSP(assignment, 0, 0, success => result = success));

        if (result)
        {
            var final = new int[assignment.Length];
            Array.Copy(assignment, final, assignment.Length);
            ReturnPooledArray(assignment);
            callback(true, final);
        }
        else
        {
            callback(false, null);
            ReturnPooledArray(assignment);
        }

    }

    private IEnumerator SolveCSPCoroutine(IEnumerator solver)
    {
        yield return solver;
    }

    private IEnumerator BacktrackCSP(int[] assignment, int assignedCount, int depth, Action<bool> callback)
    {
        if (depth > MAX_RECURSION_DEPTH)
        {
            callback(false);
            yield break;
        }

        if (assignedCount == _activeTileCount)
        {
            callback(IsValidSolution(assignment));
            yield break;
        }

        var nextVar = SelectNextVariableMRV(assignment);

        if (nextVar == -1)
        {
            callback(false);
            yield break;
        }

        var orderedValues = GetOrderedDomainValues(nextVar, assignment);

        foreach (int value in orderedValues)
        {
            if (IsConsistent(nextVar, value, assignment))
            {
                assignment[nextVar] = value;
                _valueCounts[value]++;

                var savedDomains = SaveDomainsToStack();
                var consistent = ForwardCheck(nextVar, value, assignment);

                if (consistent)
                {
                    var result = false;
                    yield return BacktrackCSP(assignment, assignedCount + 1, depth + 1, success => result = success);

                    if (result)
                    {
                        callback(true);
                        yield break;
                    }
                }

                assignment[nextVar] = -1;
                _valueCounts[value]--;
                RestoreDomainsFromStack();
            }

            if (depth % YIELD_FREQUENCY == 0)
                yield return null;
        }

        callback(false);
    }

    private int SelectNextVariableMRV(int[] assignment)
    {
        var minDomainSize = int.MaxValue;
        var candidates = new List<int>();

        for (int i = 0; i < _activeTileCount; i++)
        {
            if (assignment[i] == -1)
            {
                var domainSize = _domains[i].PopCount();
                if (domainSize < minDomainSize)
                {
                    minDomainSize = domainSize;
                    candidates.Clear();
                    candidates.Add(i);
                }
                else if (domainSize == minDomainSize)
                {
                    candidates.Add(i);
                }
            }
        }

        if (candidates.Count == 0) return -1;

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }


    private List<int> GetOrderedDomainValues(int variable, int[] assignment)
    {
        var values = new List<int>();

        for (int i = 1; i <= 9; i++)
        {
            if (_domains[variable].HasBit(i))
                values.Add(i);
        }

        values.Sort((a, b) => _valueCounts[a].CompareTo(_valueCounts[b]));

        return values;
    }

    private bool IsConsistent(int variable, int value, int[] assignment)
    {
        foreach (int neighbor in _neighborCache[variable])
        {
            if (assignment[neighbor] != -1)
            {
                var neighborValue = assignment[neighbor];
                var isRequiredPair = IsRequiredPair(variable, neighbor);

                if (!isRequiredPair)
                {
                    if (neighborValue == value || neighborValue + value == 10)
                        return false;
                }
                else
                {
                    if (neighborValue != value && neighborValue + value != 10)
                        return false;
                }
            }
        }

        var maxAllowed = Mathf.CeilToInt(_activeTileCount / 9.0f) + 1;
        return _valueCounts[value] < maxAllowed;
    }

    private bool IsRequiredPair(int pos1, int pos2)
    {
        foreach (var pair in _requiredPairs)
        {
            if ((pair.Pos1 == pos1 && pair.Pos2 == pos2) ||
                (pair.Pos1 == pos2 && pair.Pos2 == pos1))
                return true;
        }
        return false;
    }

    private BitSet[] SaveDomainsToStack()
    {
        var saved = new BitSet[_domains.Length];

        for (int i = 0; i < _domains.Length; i++)
        {
            saved[i] = new BitSet(_domains[i].Value);
        }

        _domainStack.Push(saved);
        return saved;
    }

    private bool ForwardCheck(int variable, int value, int[] assignment)
    {
        foreach (int neighbor in _neighborCache[variable])
        {
            if (assignment[neighbor] == -1)
            {
                var isRequiredPair = IsRequiredPair(variable, neighbor);

                if (!isRequiredPair)
                {
                    _domains[neighbor].ClearBit(value);
                    if (value <= 9) _domains[neighbor].ClearBit(10 - value);
                }

                if (_domains[neighbor].IsEmpty())
                    return false;
            }
        }

        return true;
    }

    private void RestoreDomainsFromStack()
    {
        if (_domainStack.Count > 0)
        {
            var saved = _domainStack.Pop();
            for (int i = 0; i < _domains.Length; i++)
            {
                _domains[i] = saved[i];
            }
        }
    }

    private bool IsValidSolution(int[] assignment)
    {
        foreach (var pair in _requiredPairs)
        {
            var val1 = assignment[pair.Pos1];
            var val2 = assignment[pair.Pos2];

            if (val1 != val2 && val1 + val2 != 10)
                return false;
        }

        return true;
    }

    private int GetPairCount(int stage) => stage switch
    {
        1 => 3,
        2 => 2,
        _ => 1
    };

    private int[] GetPooledArray()
    {
        if (_arrayPool.Count > 0)
        {
            var array = _arrayPool.Dequeue();
            Array.Clear(array, 0, array.Length);
            return array;
        }

        return new int[_activeTileCount];
    }

    private void ReturnPooledArray(int[] array)
    {
        if (_arrayPool.Count < 10)
            _arrayPool.Enqueue(array);
    }

    public void Clear()
    {
        _domainStack.Clear();
        _arrayPool.Clear();
        _requiredPairs.Clear();
    }
}