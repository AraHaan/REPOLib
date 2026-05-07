using System.Collections.Generic;
using System.Linq;
using Photon.Pun;

namespace REPOLib.Objects;

internal class PlayerCosmeticsModded : MonoBehaviourPun
{
	public List<string> cosmeticEquipped = new();

	[PunRPC]
	internal void SetupCosmeticsModdedRPC(string _cosmeticEquippedNames)
	{
		cosmeticEquipped = string.IsNullOrEmpty(_cosmeticEquippedNames) ? [] : _cosmeticEquippedNames.Split("\x1F").ToList();
	}
}