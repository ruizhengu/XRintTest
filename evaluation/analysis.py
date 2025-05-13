import pandas as pd
import matplotlib.pyplot as plt
from matplotlib.ticker import PercentFormatter

scenes = [
    "VR Template"
]

target_scene = scenes[0]
# Read both sheets
df_interactobot = pd.read_excel('results.xlsx', sheet_name=f'{target_scene}-Interactobot')
df_random = pd.read_excel('results.xlsx', sheet_name=f'{target_scene}-Interactobot_rand')

# Convert coverage to percentage
df_interactobot['Coverage'] = df_interactobot['Coverage'].astype(float)*100
df_random['Coverage'] = df_random['Coverage'].astype(float)*100

# Define the time points you want to label and their new labels
time_points = [60, 120, 180, 240, 300]
labels = [1, 2, 3, 4, 5]

plt.figure(figsize=(10, 6))
# Plot both lines with different colors and add labels for legend
plt.plot(df_interactobot["Time"], df_interactobot["Coverage"],
         linestyle='-', color='slateblue', linewidth=2, label='$Interactobot$')
plt.plot(df_random["Time"], df_random["Coverage"],
         linestyle='--', color='darkorange', linewidth=2, label='$Interactobot_{rand}$')

plt.ylim(0, 100)
plt.gca().yaxis.set_major_formatter(PercentFormatter())

# Set custom x-ticks and labels
plt.xticks(time_points, labels)

# Add legend
plt.legend()

plt.grid(axis='y', linestyle='--', alpha=0.6)
ax = plt.gca()
ax.spines['top'].set_visible(False)
# ax.spines['bottom'].set_visible(False)
ax.spines['right'].set_visible(False)
# ax.spines['left'].set_visible(False)

plt.tight_layout()
# Save the plot with 300 DPI
plt.savefig(f'{target_scene}_efficiency.png', dpi=300, bbox_inches='tight')
plt.show()
