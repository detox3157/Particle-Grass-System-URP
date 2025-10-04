using System;
using System.Collections.Generic;
using UnityEngine;

namespace ParticleGrass
{
    public class GrassInteractor : MonoBehaviour
    {
        internal static HashSet<GrassInteractor> Interactors { get; private set; } = new HashSet<GrassInteractor>();

        void OnEnable()
        {
            Interactors.Add(this);
        }
        
        void OnDisable()
        {
            Interactors.Remove(this);
        }

        private void Update()
        {
            
        }
    }
}
