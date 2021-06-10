using System;
using Unity.Mathematics;

public struct NodeCost : IEquatable<NodeCost>, IComparable<NodeCost> {
	public int3 idx;
	public int gCost;
	public int hCost;
	public int3 origin;

	public NodeCost (int3 i, int3 origin) {
		this.idx = i;
		this.origin = origin;
		this.gCost = 0;
		this.hCost = 0;
	}

	public int CompareTo (NodeCost other) {
		int compare = fCost().CompareTo(other.fCost());
		if(compare == 0) {
			compare = hCost.CompareTo(other.hCost);
		}
		return -compare;
	}

	public bool Equals (NodeCost other) {
		var b = (this.idx == other.idx);
		return math.all(b);
	}

	public int fCost () {
		return gCost + hCost;
	}

	public override int GetHashCode () {
		return idx.GetHashCode();
	}
}