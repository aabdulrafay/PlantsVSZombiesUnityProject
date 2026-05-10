using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardSelect : MonoBehaviour
{

    public GameObject[] cards;
    public int maxCardNumber;
    [Header("Auto Select")]
    public bool autoSelectAndStart = false;
    public int autoSelectCount = 2;
    public bool hideDialogDuringAuto = true;

    private float xOffset = 1.1f, yOffset = 0.6f;
    private ArrayList selectedCards=new ArrayList();
    private ArrayList barCardList=new ArrayList();
    private GameObject gameController;
    private GameObject carBar;

    void Awake()
    {
        gameController = GameObject.Find("GameController");
        if (gameController == null)
        {
            Debug.LogWarning("CardSelect: GameController object not found in scene.");
        }

        carBar = GameObject.Find("Cards");
        if (carBar == null)
        {
            Debug.LogWarning("CardSelect: Cards container object not found in scene.");
        }

        Transform text = transform.Find("Text");
        if (text != null)
        {
            var meshRenderer = text.GetComponent<MeshRenderer>();
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (meshRenderer != null && spriteRenderer != null)
            {
                meshRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
            }

            var textMesh = text.GetComponent<TextMesh>();
            if (textMesh != null)
            {
                textMesh.text += "<color=yellow>" + maxCardNumber + "</color>";
            }
            else
            {
                Debug.LogWarning("CardSelect: Text child does not have a TextMesh component.");
            }
        }
        else
        {
            Debug.LogWarning("CardSelect: Could not find child named 'Text'.");
        }

        // Instantiate selectable cards early in Awake so they never get a visible frame
        Transform container = transform.Find("CardContainer");
        if (container != null)
        {
            if (autoSelectAndStart && gameController != null)
            {
                var gc = gameController.GetComponent<GameController>();
                if (gc != null)
                {
                    gc.skipCardSelectionUI = true;
                }
            }

            for (int i = 0; i < cards.Length; i++)
            {
                float x = (i % 4) * xOffset;
                float y = -(i / 4) * yOffset;
                GameObject card = Instantiate(cards[i]);
                card.transform.parent = container;
                card.transform.localPosition = new Vector3(x, y, 0);
                var cardComp = card.GetComponent<Card>();
                if (cardComp != null) cardComp.enabled = false;
                card.tag = "SelectingCard";

                // Only hide selection cards during auto-start; normal play should keep them visible.
                if (autoSelectAndStart)
                {
                    var sr = card.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.enabled = false;
                    var mesh = card.GetComponent<MeshRenderer>();
                    if (mesh != null) mesh.enabled = false;
                }
            }

            if (autoSelectAndStart)
            {
                if (gameController != null)
                {
                    var gc = gameController.GetComponent<GameController>();
                    if (gc != null)
                    {
                        if (gc.BtnSubmitObj != null) gc.BtnSubmitObj.SetActive(false);
                        if (gc.cardDialog != null) gc.cardDialog.SetActive(false);
                    }
                }

                AutoSelectAndSubmit(autoSelectCount);
            }
        }
        else
        {
            Debug.LogWarning("CardSelect: CardContainer not found in Awake.");
        }
    }

    void Start()
    {
        // Intentionally left empty. Instantiation and auto-select occur in Awake to avoid any visible frame.
    }

    IEnumerator AutoSelectAndSubmitDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        AutoSelectAndSubmit(autoSelectCount);
    }

    public void AutoSelectAndSubmit(int count)
    {
        if (count <= 0) return;

        // find selectable cards under CardContainer
        Transform container = transform.Find("CardContainer");
        if (container == null)
        {
            Debug.LogWarning("CardSelect: CardContainer not found for auto-select.");
            return;
        }

        int selected = 0;
        foreach (Transform child in container)
        {
            if (selected >= count) break;
            GameObject go = child.gameObject;
            if (go != null && go.tag == "SelectingCard")
            {
                if (!selectedCards.Contains(go))
                {
                    selectedCards.Add(go);
                    var cardComp = go.GetComponent<Card>();
                    if (cardComp != null)
                        cardComp.SetSprite(false);
                    selected++;
                }
            }
        }

        if (selected > 0)
        {
            UpdateCardBar();
            Submit();
        }
        else
        {
            Debug.LogWarning("CardSelect: No selectable cards found to auto-select.");
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Collider2D collider = Physics2D.OverlapPoint(Utility.GetMouseWorldPos());
            if (collider != null)
            {
                if (collider.gameObject.tag == "SelectingCard")
                {
                    GameObject card = collider.gameObject;
                    if (selectedCards.Contains(card))
                    {
                        selectedCards.Remove(card);
                        card.GetComponent<Card>().SetSprite(true);
                        UpdateCardBar();
                    }
                    else if (selectedCards.Count < maxCardNumber)
                    {
                        selectedCards.Add(card);
                        card.GetComponent<Card>().SetSprite(false);
                        UpdateCardBar();
                    }
                }
            }
        }
    }

    void UpdateCardBar()
    {
        RemoveAllBarCards();
        float xOff = -0.8f;
        for (int i = 0; i < selectedCards.Count; i++)
        {
            GameObject prefab=selectedCards[i] as GameObject;
            GameObject card = Instantiate(prefab);
            card.tag = "Card";
            card.transform.parent = carBar.transform;
            card.transform.localPosition=new Vector3(0, i*xOff, 0);

            var sr = card.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = true;
            var mesh = card.GetComponent<MeshRenderer>();
            if (mesh != null) mesh.enabled = true;

            barCardList.Add(card);
        }
    }

    void RemoveAllBarCards()
    {
        object[] barCards = barCardList.ToArray();
        foreach (GameObject card in barCards)
        {
            if (card != null)
                Destroy(card);
        }
        barCardList.Clear();
    }

    public void Submit()
    {
        foreach (GameObject card in barCardList)
        {
            if (card != null)
            {
                var c = card.GetComponent<Card>();
                if (c != null)
                    c.enabled = true;
            }
        }

        if (gameController != null)
        {
            var gc = gameController.GetComponent<GameController>();
            if (gc != null)
                gc.AfterSelectedCard();
            else
                Debug.LogWarning("CardSelect: GameController component not found on GameController object.");
        }
        else
        {
            Debug.LogWarning("CardSelect: gameController reference is null on Submit().");
        }
    }

    public void Reset()
    {
        foreach (GameObject card in selectedCards)
        {
            card.GetComponent<Card>().SetSprite(true);
        }
        selectedCards.Clear();
        RemoveAllBarCards();
    }
}
