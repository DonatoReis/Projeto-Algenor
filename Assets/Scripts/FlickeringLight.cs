using UnityEngine;

public class FlickeringLight : MonoBehaviour
{
    public Light flickeringLight; // Referência à luz
    public float minBlinkInterval = 0.1f; // Intervalo mínimo entre piscadas
    public float maxBlinkInterval = 0.5f; // Intervalo máximo entre piscadas
    public float minLightIntensity = 0.2f; // Intensidade mínima da luz
    public float maxLightIntensity = 1.5f; // Intensidade máxima da luz

    private bool isLightOn = true;

    void Start()
    {
        if (flickeringLight == null)
        {
            flickeringLight = GetComponent<Light>();
        }

        // Inicia o primeiro ciclo de piscada
        StartCoroutine(Flicker());
    }

    System.Collections.IEnumerator Flicker()
    {
        while (true)
        {
            // Alterna o estado da luz
            isLightOn = !isLightOn;

            // Define uma intensidade de luz aleatória (para simular um curto)
            if (isLightOn)
            {
                flickeringLight.intensity = Random.Range(minLightIntensity, maxLightIntensity);
            }
            else
            {
                flickeringLight.intensity = 0;
            }

            // Espera um tempo aleatório antes do próximo piscar
            yield return new WaitForSeconds(Random.Range(minBlinkInterval, maxBlinkInterval));
        }
    }
}
