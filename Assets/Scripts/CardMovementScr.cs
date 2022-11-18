using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

public class CardMovementScr : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{

    public CardControllerScr CC;
    Camera MainCamera;
    Vector3 offset;
    public Transform DefaultParent, DefaultTmpCardParent;
    GameObject TmpCardGO;
    public bool IsDraggable;
    int stardID;

    void Awake()
    {
        MainCamera = Camera.allCameras[0];
        TmpCardGO = GameObject.Find("TmpCardGO");
       
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        offset = transform.position - MainCamera.ScreenToWorldPoint(eventData.position);

        DefaultParent = DefaultTmpCardParent = transform.parent;

        IsDraggable = GameManagerScr.Instance.IsPlayerTurn &&
            (
            (DefaultParent.GetComponent<DropPlaceScr>().Type == FieldType.SELF_HAND && GameManagerScr.Instance.CurrentGame.Player.Mana >= CC.Card.Manacost) ||
            (DefaultParent.GetComponent<DropPlaceScr>().Type == FieldType.SELF_FIELD && CC.Card.CanAttack)
            );
            
          

        if (!IsDraggable)
            return;
        stardID = transform.GetSiblingIndex();
        if(CC.Card.IsSpell || CC.Card.CanAttack)
        GameManagerScr.Instance.HighlightTargets(CC,true);

        TmpCardGO.transform.SetParent(DefaultParent);
        TmpCardGO.transform.SetSiblingIndex(transform.GetSiblingIndex());

        transform.SetParent(DefaultParent.parent);
        GetComponent<CanvasGroup>().blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsDraggable)
            return;

        Vector3 newPos = MainCamera.ScreenToWorldPoint(eventData.position);
        transform.position = newPos + offset;

        if (!CC.Card.IsSpell)
        {
            if (TmpCardGO.transform.parent != DefaultTmpCardParent)
                TmpCardGO.transform.SetParent(DefaultTmpCardParent);
            if (DefaultParent.GetComponent<DropPlaceScr>().Type != FieldType.SELF_HAND)
                CheckPosition();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!IsDraggable)
            return;

        GameManagerScr.Instance.HighlightTargets(CC,false);

        transform.SetParent(DefaultParent);
        GetComponent<CanvasGroup>().blocksRaycasts = true;

        transform.SetSiblingIndex(TmpCardGO.transform.GetSiblingIndex());
        TmpCardGO.transform.SetParent(GameObject.Find("Canvas").transform);
        TmpCardGO.transform.localPosition = new Vector3(1200, 0);
    }

    void CheckPosition()
    {
        int newIndex = DefaultTmpCardParent.childCount;

        for (int i = 0; i < DefaultTmpCardParent.childCount; i++)
        {
            if (transform.position.x < DefaultTmpCardParent.GetChild(i).position.x)
            {
                newIndex = i;

                if (TmpCardGO.transform.GetSiblingIndex() < newIndex)
                    newIndex--;

                break;
            }
        }
        if (TmpCardGO.transform.parent == DefaultParent)
            newIndex = stardID;

        TmpCardGO.transform.SetSiblingIndex(newIndex);
    }

    public void MovToField(Transform field)
    {
        transform.SetParent(GameObject.Find("Canvas").transform);
        transform.DOMove(field.position, .5f);
    }

    public void MoveToTarget(Transform target)
    {
        StartCoroutine(MoveToTargetCor(target));
    }

    IEnumerator MoveToTargetCor(Transform target)
    {
        Vector3 pos = transform.position;
        Transform parent = transform.parent;
        int index = transform.GetSiblingIndex();
        if(transform.parent.GetComponent<HorizontalLayoutGroup>())
        transform.parent.GetComponent<HorizontalLayoutGroup>().enabled = false;

        transform.SetParent(GameObject.Find("Canvas").transform);
        transform.DOMove(target.position, .25f);
        yield return new WaitForSeconds(.25f);
        transform.DOMove(pos, .25f);
        yield return new WaitForSeconds(.25f);
        transform.SetParent(parent);
        transform.SetSiblingIndex(index);
        if(transform.parent.GetComponent<HorizontalLayoutGroup>())
        transform.parent.GetComponent<HorizontalLayoutGroup>().enabled = true;

    }
}
