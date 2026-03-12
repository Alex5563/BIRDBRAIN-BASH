using System.Collections.Generic;
using UnityEngine;

public static class DataTransferManager
{
    // which control scheme each human player will use
    public static List<bool> isKBMInput;

    // Which bird each human player has chosen. The list matches isKBMInput
    public static List<BirdType> selectedBirds;
}
