import sys
from statistics import mean, median, variance, stdev
import matplotlib.pyplot as plt

def draw_iron(data):
  n = len(data)
  total = sum(data)
  maximum = max(data)
  mu = mean(data)
  med = median(data)
  sdev = stdev(data)
  plt.rcParams['font.family'] = 'Kozuka Gothic Pro'
  
  fig, ax = plt.subplots()
  edges = range(0,100,5)
  _, bins, patches = ax.hist(data, bins=edges)
  ax.set_xlim([0, 100])
#  plt.title('アイマストドンユーザ分布( ' + date + ' )')
#  plt.xlabel('%sのトゥート数' % date)
  plt.ylabel('人数')
  plt.text(0.6, 0.8, '$n=%d,\\ sum=%d,$\n$med=%d,\\ \\mu=%.2f,\\ \\sigma=%.2f$' % (n, total, med, mu, sdev), transform=ax.transAxes)
  plt.text(0.5, 0.95, '当日1トゥート以上のアカウントが対象。', transform=ax.transAxes)
  plt.text(0.5, 0.9, '公開トゥートのみ対象。', transform=ax.transAxes)
  plt.show()
  
def draw_old(argv):
  date = argv.pop(0)
  data = [int(i) for i in argv]
  
  n = len(data)
  total = sum(data)
  maximum = max(data)
  mu = mean(data)
  med = median(data)
  sdev = stdev(data)
  plt.rcParams['font.family'] = 'Kozuka Gothic Pro'
  
  fig, ax = plt.subplots()
  edges = range(0, (maximum // 100 + 1) * 100, 10)
  #_, bins, patches = ax.hist(data, bins=edges)
  _, bins, patches = ax.hist(data, bins=n)
  ax.set_ylim([0, 100])
  #ax.set_xlim([0, (maximum // 100 + 1) * 100])
  plt.title('アイマストドンユーザ分布( ' + date + ' )')
  plt.xlabel('%sのトゥート数' % date)
  plt.ylabel('人数')
  plt.text(0.6, 0.8, '$n=%d,\\ sum=%d,$\n$med=%d,\\ \\mu=%.2f,\\ \\sigma=%.2f$' % (n, total, med, mu, sdev), transform=ax.transAxes)
  plt.text(0.5, 0.95, '当日1トゥート以上のアカウントが対象。', transform=ax.transAxes)
  plt.text(0.5, 0.9, '公開トゥートのみ対象。', transform=ax.transAxes)
  plt.show()

def draw(date, argv):
  time = argv.pop(0)
  data = [int(i) for i in argv]
  
  n = len(data)
  total = sum(data)
  maximum = max(data)
  mu = mean(data)
  med = median(data)
  sdev = stdev(data)
  plt.rcParams['font.family'] = 'Kozuka Gothic Pro'

  fig, ax = plt.subplots()
  edges = range(0,100,5)
  _, bins, patches = ax.hist(data, bins=edges)
  plt.title('アイマストドンユーザ分布(' + date + ' ' + time + '現在)')
  plt.xlabel('今日のトゥート数')
  plt.ylabel('人数')
  plt.text(0.6, 0.8, '$n=%d,\\ sum=%d,$\n$med=%d,\\ \\mu=%.2f,\\ \\sigma=%.2f$' % (n, total, med, mu, sdev), transform=ax.transAxes)
  plt.text(0.5, 0.95, '今日1トゥート以上のアカウントが対象。', transform=ax.transAxes)
  plt.text(0.5, 0.9, '公開トゥートのみ対象。', transform=ax.transAxes)
  plt.show()
  
if __name__ == "__main__":
  try:
    argv = sys.argv
    argv.pop(0)
    argv_1 = argv.pop(0)
    if argv_1 == "old":
      draw_old(argv)
    else:
      draw(argv_1, argv)
  except:
    import traceback
    traceback.print_exc()
    input('>> ')
    