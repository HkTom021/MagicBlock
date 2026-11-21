using System;

namespace MagicBlock
{
    /// <summary>
    /// Đếm lượt chính và màn (hiển thị "lượt - màn" như 1 - 7). Mỗi lượt có <see cref="StagesPerRound"/> màn,
    /// màn cuối là màn thử thách. Xong màn <see cref="BuffStage"/> và xong màn thử thách thì được chọn buff.
    /// </summary>
    public sealed class RunProgress
    {
        public int StagesPerRound { get; }
        public int BuffStage { get; }

        /// <summary>Lượt chính hiện tại, bắt đầu từ 1.</summary>
        public int Round { get; private set; }
        /// <summary>Màn hiện tại trong lượt, từ 1 tới StagesPerRound.</summary>
        public int Stage { get; private set; }
        public int StagesCompleted { get; private set; }

        public bool IsChallenge => Stage == StagesPerRound;

        public RunProgress(int stagesPerRound, int buffStage)
        {
            if (stagesPerRound < 2) throw new ArgumentOutOfRangeException(nameof(stagesPerRound), "Cần ít nhất 2 màn mỗi lượt");
            if (buffStage < 1 || buffStage >= stagesPerRound) throw new ArgumentOutOfRangeException(nameof(buffStage));
            StagesPerRound = stagesPerRound;
            BuffStage = buffStage;
            Reset();
        }

        public void Reset()
        {
            Round = 1;
            Stage = 1;
            StagesCompleted = 0;
        }

        /// <summary>Xong màn hiện tại có được chọn buff không.</summary>
        public bool IsBuffStage(int stage) => stage == BuffStage || stage == StagesPerRound;

        /// <summary>Số màn còn lại tính cả màn hiện tại tới mốc chọn buff kế tiếp (1 = xong màn này là được).</summary>
        public int StagesUntilBuff => (Stage <= BuffStage ? BuffStage : StagesPerRound) - Stage + 1;

        public void Advance()
        {
            StagesCompleted++;
            Stage++;
            if (Stage > StagesPerRound)
            {
                Stage = 1;
                Round++;
            }
        }
    }
}
