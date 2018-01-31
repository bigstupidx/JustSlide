using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SgLib;
using System;


public class EnvironmentManager : MonoBehaviour
{
    [System.Serializable]
    public class Environment
    {
        public Character character;
        public GameObject[] trees;
        public GameObject[] obstacles;
        public bool useCustomBackgroundMusic;
        public Sound backgroundMusic = null;
        public Color32 planeColor;
        public bool enableFence;
        public GameObject fencePrefab = null;
        public bool enableBirds;
        public GameObject[] birdPrefabs;
        public bool enableFallingParticle;
        public GameObject fallingParticle;
        public int numberOfObjectInAWall = 7;
        public Color skyboxTopColor = Color.blue;
        public Color skyboxBottomColor = Color.blue;
        public GameObject mainHouse;
    }

    public Environment defaultEnvironment;
    public List<Environment> environmentList = new List<Environment>();

    public Environment GetCurrentEvironment()
    {
        return GetEnvironmentForCharacter(HumanManager.Instance.humans[HumanManager.Instance.CurrentHumanIndex].GetComponent<Character>());
    }

    public Environment GetEnvironmentForCharacter(Character character)
    {
        if (character != null)
        {
            foreach (var environment in environmentList)
            {
                if (environment.character != null && environment.character.characterName.Equals(character.characterName, StringComparison.OrdinalIgnoreCase))
                {
                    return environment;
                }
            }
        }

        return defaultEnvironment;
    }
}
