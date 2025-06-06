import pandas as pd
import matplotlib.pyplot as plt
from matplotlib.ticker import PercentFormatter

scenes = [
    "VR Template",
    "XRI Assets",
    "XRI Kit",
    "XRI Examples",
    "GameJam",
    "EscapeProto",
    "EscapeRoom",
    "Average"
]

# Define the time points you want to label and their new labels
time_points = [60, 120, 180, 240, 300, 360, 420, 480, 540, 600]
labels = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
x_labels = [
    "Scene 1",
    "Scene 2",
    "Scene 3",
    "Scene 4",
    "Scene 5",
    "Scene 6",
    "Scene 7",
    "Average"
]

fig, axes = plt.subplots(2, 4, figsize=(26, 10))
axes = axes.flatten()

for idx, target_scene in enumerate(scenes[:8]):
    ax = axes[idx]
    # Read both sheets
    df_interactobot = pd.read_excel('results.xlsx', sheet_name=f'{target_scene}-InteractoBot')

    df_random = pd.read_excel('results.xlsx', sheet_name=f'{target_scene}-Random')
    df_interactobot['Coverage'] = df_interactobot['Coverage'].astype(float)*100
    df_random['Coverage'] = df_random['Coverage'].astype(float)*100

    run_cols_interactobot = [col for col in df_interactobot.columns if col.startswith('Run')]
    run_cols_random = [col for col in df_random.columns if col.startswith('Run')]
    df_interactobot[run_cols_interactobot] = df_interactobot[run_cols_interactobot].astype(float) * 100
    df_random[run_cols_random] = df_random[run_cols_random].astype(float) * 100

    # Calculate min/max for confidence band
    min_interactobot = df_interactobot[run_cols_interactobot].min(axis=1)
    max_interactobot = df_interactobot[run_cols_interactobot].max(axis=1)
    min_random = df_random[run_cols_random].min(axis=1)
    max_random = df_random[run_cols_random].max(axis=1)

    # Plot mean lines (using Coverage column)
    ax.plot(df_interactobot["Time"], df_interactobot["Coverage"],
            linestyle='-', color='slateblue', linewidth=5, label='XRintTest')
    ax.plot(df_random["Time"], df_random["Coverage"], linestyle='--',
            color='darkorange', linewidth=5, label='Random Baseline')

    # Plot confidence bands
    ax.fill_between(df_interactobot["Time"], min_interactobot, max_interactobot, color='slateblue', alpha=0.2)
    ax.fill_between(df_random["Time"], min_random, max_random, color='darkorange', alpha=0.2)

    ax.set_ylim(0, 100)
    ax.yaxis.set_major_formatter(PercentFormatter())

    ax.set_xticks(time_points)
    ax.set_xticklabels(labels, fontsize=18)
    ax.tick_params(axis='y', labelsize=18)

    ax.grid(axis='y', linestyle='--', alpha=0.6)
    ax.spines['top'].set_visible(False)
    ax.spines['right'].set_visible(False)

    ax.set_title(x_labels[idx], fontsize=20, fontweight='bold')
    ax.set_xlabel('Time (minutes)', fontsize=20)
    ax.set_ylabel('XUI Coverage (%)', fontsize=20)

# Hide any unused subplots
for j in range(len(scenes), 8):
    fig.delaxes(axes[j])

# Use handles and labels from the first subplot for the legend
handles, labels = ax.get_legend_handles_labels()
fig.legend(handles, labels, fontsize=22, loc='lower center', ncol=2)

plt.tight_layout(rect=[0, 0.08, 1, 1])
plt.savefig(f'RQ3_efficiency.png', dpi=300, bbox_inches='tight')
plt.show()
