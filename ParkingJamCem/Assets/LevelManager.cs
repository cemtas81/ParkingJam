using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class LevelManager : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private List<GameObject> carList = new List<GameObject>();
    [SerializeField] private ParticleSystem particle;
    private void Start()
    {
        Application.targetFrameRate = 60;

        CarMovement[] carMovements = FindObjectsOfType<CarMovement>();
        foreach (CarMovement item in carMovements)
        {
            carList.Add(item.gameObject);
        }

    }
    public void CarFinished(GameObject car)
    {
        carList.Remove(car);
        if (carList.Count == 0)
        {
            StartCoroutine(AllFinished());
        }
    }
    IEnumerator AllFinished()
    {
        yield return new WaitForSeconds(1f);
        particle.Play();
        Congrats();
        yield return new WaitForSeconds(3);
        NextLevel();
    }
    void Congrats()
    {
        canvas.enabled = true;
    }
    public void NextLevel()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        if (currentSceneIndex + 1 < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(currentSceneIndex + 1);
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }
    public void Restart()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        SceneManager.LoadScene(currentSceneIndex);
    }
}
