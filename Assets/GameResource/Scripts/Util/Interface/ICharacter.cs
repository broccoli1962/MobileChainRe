namespace Backend.Util.Interface
{
    public interface ICharacter
    {
        int Id { get; }

        /// <summary>
        /// 슬롯 이동 시 호출. 이전 슬롯 인덱스(0-based)와 새 슬롯 인덱스를 전달한다.
        /// </summary>
        void OnSlotChanged(int fromSlot, int toSlot);
    }
}
