package com.example.test;

import java.util.Timer;
import java.util.TimerTask;

class ObjectTimer {
    // Timer
    private Timer timer;
    // TimerTask
    private TimerTask timerTask;
    // 타이머 스피드
    private int speed;
    // 타이머 Max
    private int max;
    // 현재 cost
    private int cost;

    public int getSpeed() {
        return speed;
    }
    public int getMax() {
        return max;
    }
    public int getCost() {
        return cost;
    }
    public Timer getTimer() {
        return timer;
    }
    public TimerTask getTimerTask() {
        return timerTask;
    }
    public void setSpeed(int speed) {
        this.speed = speed;
    }
    public void setMax(int max) {
        this.max = max;
    }
    public void setCost(int cost) {
        this.cost = cost;
    }
    public void setTimer(Timer timer) {
        this.timer = timer;
    }
    public void setTimerTask(TimerTask timerTask) {
        this.timerTask = timerTask;
    }

    public ObjectTimer(int speed, int max, int cost) {
        this.timer = null;
        this.timerTask = null;
        this.speed = speed;
        this.max = max;
        this.cost = cost;
    }
}
