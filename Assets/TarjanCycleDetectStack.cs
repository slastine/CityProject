using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TarjanCycleDetectStack
{
    protected List<List<RoadSegment>> _StronglyConnectedComponents;
    protected Stack<RoadSegment> _Stack;
    protected int _Index;

    public List<List<RoadSegment>> DetectCycle(List<RoadSegment> graph_nodes)
    {
        _StronglyConnectedComponents = new List<List<RoadSegment>>();

        _Index = 0;
        _Stack = new Stack<RoadSegment>();

        foreach (RoadSegment v in graph_nodes)
        {
            if (v.Index < 0)
            {
                StronglyConnect(v);
            }
        }

        return _StronglyConnectedComponents;
    }

    private void StronglyConnect(RoadSegment v)
    {
        v.Index = _Index;
        v.Lowlink = _Index;

        _Index++;
        _Stack.Push(v);

        foreach (RoadSegment w in v.Dependencies)
        {
            if (w.Index < 0)
            {
                StronglyConnect(w);
                v.Lowlink = Mathf.Min(v.Lowlink, w.Lowlink);
            }
            else if (_Stack.Contains(w))
            {
                v.Lowlink = Mathf.Min(v.Lowlink, w.Index);
            }
        }

        if (v.Lowlink == v.Index)
        {
            List<RoadSegment> cycle = new List<RoadSegment>();
            RoadSegment w;

            do
            {
                w = _Stack.Pop();
                cycle.Add(w);
            } while (v != w);

            _StronglyConnectedComponents.Add(cycle);
        }
    }
}
