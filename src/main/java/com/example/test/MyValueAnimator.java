package com.example.test;

import android.animation.ValueAnimator;

import java.util.Timer;

// ValueAnimator를 상속받아 pause / resume 기능을 추가한 Animator
public class MyValueAnimator extends ValueAnimator {
    public float animatedValue = 0;
    private long currenttime;
    private long totaltime;
    private boolean onetime = true;
    private volatile boolean mPaused = false;
    private Timer t;

    public boolean getMPaused() {

        return mPaused;
    }

    public void setAnimatedValue(float animatedValue) {
        this.animatedValue = animatedValue;
    }
    public void setCurrenttime(long currenttime) {
        this.currenttime = currenttime;
    }
    public void setTotaltime(long totaltime) {
        this.totaltime = totaltime;
    }

    public long getCurrenttime() {
        return currenttime;
    }

    public void pause() {
        if(!mPaused) {
            animatedValue = (float) getAnimatedValue();
            mPaused = true;
        }
    }

    @Override
    public Object getAnimatedValue() {
        if (mPaused) {
            if(onetime){
                currenttime = getCurrentPlayTime();
                //   totaltime = getStartDelay()+ (getDuration() * (getRepeatCount() + 1));
                totaltime = getDuration();
                setDuration(Long.parseLong("9999999999999999"));
                onetime =false;
            }
            return animatedValue;
        }
        return super.getAnimatedValue();
    }

    public void resume() {
        if(mPaused) {
            mPaused = false;
            onetime=true;
            setCurrentPlayTime(currenttime);
            setDuration(totaltime);
        }
    }

    public MyValueAnimator(float from, float to) {
        setFloatValues(from, to);
    }

}