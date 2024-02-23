using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Thirdweb;
using UnityEngine.SceneManagement;

public class TokenScript : MonoBehaviour
{
    // public GemCollectScript gemCollectScript;
    public GameObject HasNotClaimedState;
    public GameObject ClaimingState;
    public GameObject HasClaimedState;
    public int gemsToClaim;
    // [SerializeField] public TMPro.TextMeshProUGUI gemsEarnedText;
    [SerializeField] private TMPro.TextMeshProUGUI tokenBalanceText;
    private const string DROP_ERC20_CONTRACT = "0x0C71eA1115746f3A41eEc52D2aE8B99f87ACE65c";
    private void Start()
    {
        HasNotClaimedState.SetActive(true);
        ClaimingState.SetActive(false);
        HasClaimedState.SetActive(false);
    }
    private void Update()
    {
        // gemsEarnedText.text = "Gems Earned: " + gemCollectScript.gems.ToString();
        gemsToClaim = 20;
    }
    // Start is called before the first frame update
    public async void GetTokenBlance()
    {
        try
        {
            var address = await ThirdwebManager.Instance.SDK.wallet.GetAddress();
            Contract contract = ThirdwebManager.Instance.SDK.GetContract(DROP_ERC20_CONTRACT);
            var data = await contract.ERC20.BalanceOf(address);
            tokenBalanceText.text = "$TOKEN: " + data.displayValue;
        }
        catch (System.Exception)
        {
            Debug.Log("Error getting token balance");
        }
    }

    public void ResetBlance()
    {
        tokenBalanceText.text = "$GEM: 0";
    }

    public async void MintERC20()
    {
        try
        {
            Debug.Log("Minting ERC20");
            Contract contract = ThirdwebManager.Instance.SDK.GetContract(DROP_ERC20_CONTRACT);
            HasNotClaimedState.SetActive(false);
            ClaimingState.SetActive(true);
            var results = await contract.ERC20.Claim(gemsToClaim.ToString());
            Debug.Log("ERC20 minted");
            GetTokenBlance();
            ClaimingState.SetActive(false);
            HasClaimedState.SetActive(true);

        }
        catch (System.Exception)
        {
            Debug.Log("Error minting ERC20");
        }

    }

    public void RestartGame()
    {
        SceneManager.LoadScene(1);
    }
}
