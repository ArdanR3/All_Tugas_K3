using UnityEngine;

/// <summary>
/// Script Test untuk verifikasi integrasi External Script Editor (Visual Studio)
/// Praktikum Pemrograman Berbasis AR/VR - Week 02 Modul 01
/// Mahasiswa: Ardan Rizqi Pradana (0920240011)
/// </summary>
public class Test : MonoBehaviour
{
    [Header("Identitas Mahasiswa")]
    [SerializeField] private string namaMahasiswa = "Ardan Rizqi Pradana";
    [SerializeField] private string nimMahasiswa = "0920240011";

    void Start()
    {
        Debug.Log($"[ARVR] Test.cs berhasil terintegrasi dengan Visual Studio! Mahasiswa: {namaMahasiswa} ({nimMahasiswa})");
    }
}
