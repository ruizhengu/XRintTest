import pandas as pd
import matplotlib.pyplot as plt
from matplotlib.ticker import PercentFormatter

scenes = [
    "VR Template",
    "XRI Assets",
    "EscapeProto",
    "GameJam",
    "VR Template",
    "XRI Assets",
    "EscapeProto",
    "GameJam"
]

# Define the time points you want to label and their new labels
time_points = [60, 120, 180, 240, 300]
labels = [1, 2, 3, 4, 5]

fig, axes = plt.subplots(2, 4, figsize=(24, 10))
axes = axes.flatten()

for idx, target_scene in enumerate(scenes[:8]):
    ax = axes[idx]
    # Read both sheets
    df_interactobot = pd.read_excel('results.xlsx', sheet_name=f'{target_scene}-InteractoBot')
    df_random = pd.read_excel('results.xlsx', sheet_name=f'{target_scene}-Random')

    # Convert coverage to percentage
    df_interactobot['Coverage'] = df_interactobot['Coverage'].astype(float)*100
    df_random['Coverage'] = df_random['Coverage'].astype(float)*100

    # Plot both lines with different colors and add labels for legend
    ax.plot(df_interactobot["Time"], df_interactobot["Coverage"],
            linestyle='-', color='slateblue', linewidth=5, label='InteractoBot')
    ax.plot(df_random["Time"], df_random["Coverage"],
            linestyle='--', color='darkorange', linewidth=5, label='Random Baseline')

    ax.set_ylim(0, 100)
    ax.yaxis.set_major_formatter(PercentFormatter())

    ax.set_xticks(time_points)
    ax.set_xticklabels(labels, fontsize=28, fontweight='bold')
    ax.tick_params(axis='y', labelsize=28)
    for label in ax.get_yticklabels():
        label.set_fontweight('bold')

    ax.grid(axis='y', linestyle='--', alpha=0.6)
    ax.spines['top'].set_visible(False)
    ax.spines['right'].set_visible(False)
    ax.set_xlabel(target_scene, fontsize=20, fontweight='bold')

# Hide any unused subplots
for j in range(len(scenes), 8):
    fig.delaxes(axes[j])

# Use handles and labels from the first subplot for the legend
handles, labels = ax.get_legend_handles_labels()
fig.legend(handles, labels, fontsize=28, loc='lower center', bbox_to_anchor=(0.5, -0.01), ncol=2)

plt.tight_layout(rect=[0, 0.08, 1, 1])
plt.savefig(f'RQ3_efficiency.png', dpi=300, bbox_inches='tight')
plt.show()
