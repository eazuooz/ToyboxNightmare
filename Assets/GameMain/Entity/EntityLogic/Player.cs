using UnityEngine;
using UnityGameFramework.Runtime;

public class Player : EntityLogic
{
    protected internal override void OnInit(object userData)
    {
        base.OnInit(userData);
        // 초기화 코드 작성
    }

    protected internal override void OnShow(object userData)
    {
        base.OnShow(userData);
        Debug.Log("플레이어 등장");
    }

    protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        // 매 프레임 이동 또는 입력 처리
    }
}
